using System.Text;
using System.Xml.Linq;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class DocumentationSemanticsTests
{
    [Theory]
    [InlineData("@")]
    [InlineData("\\")]
    public void Parse_PreservesUsefulSectionsAndDropsCode(string prefix)
    {
        var doc = Parse("""
            Compute values.
            @param[in] values Input coordinates: 3 elements.
              A second line with @c special <values>.

              A second paragraph.
            @code
            @param fake This is sample code, not documentation.
            @return fake
            @endcode
            @param[out] result The computed value.
            @returns An array of 3 elements owned by this object.
            @seealso vtkMissing::Overloaded(int) vtkOther
            @warning The pointer is invalidated by the next call.
            @example ignored.cxx
            Discard this example description.
            """.Replace("@", prefix, StringComparison.Ordinal));
        Assert.Equal("Compute values.", doc.Summary);
        Assert.Equal(2, doc.Parameters!.Count);
        Assert.Equal("in", doc.Parameters[0].Direction);
        Assert.Contains("second paragraph", doc.Parameters[0].Text);
        Assert.Contains("special <values>", doc.Parameters[0].Text);
        Assert.Equal("out", doc.Parameters[1].Direction);
        Assert.Equal("An array of 3 elements owned by this object.", doc.Returns);
        Assert.Contains("See also: vtkMissing::Overloaded(int) vtkOther", doc.Remarks);
        Assert.Contains("Warning: The pointer is invalidated", doc.Remarks);
        Assert.DoesNotContain("Discard", doc.Remarks);
        Assert.DoesNotContain("fake", doc.ToString());
    }

    [Theory]
    [InlineData("@verbatim", "@endverbatim")]
    [InlineData("@code{.cpp}", "@endcode")]
    [InlineData("```cpp", "```")]
    [InlineData("~~~cpp", "~~~")]
    public void Parse_SkippedBlocksDoNotSwallowFollowingReturn(string start, string end)
    {
        var doc = Parse($"Summary.\n{start}\n@param fake Invalid\n{end}\n@return Useful result.");
        Assert.Null(doc.Parameters);
        Assert.Equal("Useful result.", doc.Returns);
    }

    [Fact]
    public void Parse_ParameterListsDirectionsAndMultilineSeeAlso()
    {
        var doc = Parse("@param[in,out] x,y Coordinates.\n@sa\nvtkOne vtkTwo\n@retval 0 No result.\n@retval 1 Success.");
        Assert.Null(doc.Summary);
        Assert.Equal(["x", "y"], doc.Parameters!.Select(p => p.Name));
        Assert.All(doc.Parameters!, p => Assert.Equal("in,out", p.Direction));
        Assert.Equal("See also: vtkOne vtkTwo", doc.Remarks);
        Assert.Equal("0 No result.\n\n1 Success.", doc.Returns);
    }

    [Fact]
    public void Map_RenamesParametersAndFiltersSharedGroupDocumentation()
    {
        var source = Parse("Set or get values.\n@param x Coordinate.\n@param other Other overload.\n@return Current value.");
        var function = Function("void", new WhitelistParameter { Type = "double", Name = "event" }, new WhitelistParameter { Type = "int", Name = "count" });
        var inspected = new InspectedFunction("Method", "", "void", [new("double", "x"), new("int", "n")], true, Documentation: source);
        var doc = BindingDocumentation.ForMethod(function, inspected);
        Assert.Null(doc.Returns);
        Assert.Equal(["event", "count"], doc.Parameters!.Select(p => p.Name));
        Assert.Equal("Coordinate.", doc.Parameters![0].Text);
        Assert.Equal("", doc.Parameters![1].Text);
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkBase", false, [function], new("vtkThing", [inspected]));
        Assert.Contains("double @event", text);
        Assert.Contains("<param name=\"event\">", text);
        Assert.Contains("<param name=\"count\" />", text);
        Assert.DoesNotContain("Other overload.", text);
    }

    [Fact]
    public void Map_UsesOnlyKnownParameterLengths()
    {
        var function = Function("void",
            new() { Type = "double[3]", Name = "position" },
            new() { Type = "double*", Name = "viewport", Direction = "in", Length = new() { Kind = "fixed", Value = 4 } },
            new() { Type = "const vtkIdType*", Name = "ids", Direction = "in", Length = new() { Kind = "parameter", Name = "count" } },
            new() { Type = "vtkIdType", Name = "count" });
        var doc = BindingDocumentation.ForMethod(function, null);
        Assert.Contains("3 elements", doc.Parameters![0].Text);
        Assert.Contains("4 elements", doc.Parameters[1].Text);
        Assert.Contains("specified by count", doc.Parameters[2].Text);
        Assert.Empty(doc.Parameters[3].Text);
    }

    [Theory]
    [InlineData(null, "borrows the native object")]
    [InlineData("borrowed", "borrows the native object")]
    [InlineData("owned", "owns a native reference")]
    public void Map_OwnershipMatchesGeneratedWrapper(string? ownership, string expected)
    {
        var function = Function("vtkThing*") with { Return = new() { Type = "vtkThing*", Ownership = ownership } };
        var doc = BindingDocumentation.ForMethod(function, null);
        Assert.Contains(expected, doc.Remarks);
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkBase", false, [function]);
        Assert.Contains(ownership == "owned" ? "vtkThing.TakeReference(" : "vtkThing.FromBorrowedPointer(", text);
        Assert.Contains(expected, text);
        Assert.Contains("owns a native reference", BindingDocumentation.ForNew(null).Remarks);
    }

    [Theory]
    [InlineData("const char*", "managed string")]
    [InlineData("vtkStdString", "managed string")]
    [InlineData("vtkColor3d", "C# value type")]
    public void Map_CopiedReturnsDoNotTransferNativeMemoryToCaller(string type, string description)
    {
        var doc = BindingDocumentation.ForMethod(Function(type), null);
        Assert.Contains(description, doc.Remarks);
        Assert.Contains("does not release native memory", doc.Remarks);
    }

    [Fact]
    public void Map_ReturnPointerDoesNotInventLengthOrOwnership()
    {
        var function = Function("double*");
        var source = new InspectedFunction("Method", "", "double*", [], true, Documentation: Parse("@return The result buffer."));
        var doc = BindingDocumentation.ForMethod(function, source);
        Assert.Equal("The result buffer.", doc.Returns);
        Assert.Null(doc.Remarks);
        source = source with { Documentation = Parse("@return A buffer containing 3 elements; valid until the next call.") };
        Assert.Equal(source.Documentation.Returns, BindingDocumentation.ForMethod(function, source).Returns);
    }

    [Fact]
    public void Map_ReportsExplicitOwnershipConflictWithoutChangingBinding()
    {
        var function = Function("vtkThing*");
        var source = new InspectedFunction("Method", "", "vtkThing*", [], true,
            Documentation: Parse("Create a table.\n\nThe caller is responsible for deleting the table after use."));
        var warnings = new StringWriter();
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkBase", false, [function], new("vtkThing", [source]), warnings);
        Assert.Contains("Documentation warning: vtkThing.Method", warnings.ToString());
        Assert.Contains("vtkThing.FromBorrowedPointer(", text);
    }

    [Fact]
    public void Emit_EscapesStructuredDocumentationAndUsesPlainSeeAlso()
    {
        var doc = Parse("Summary.\n@param x a < b && b > c\n@return <value> & result\n@sa vtkMissing::Unknown()");
        var text = new StringBuilder();
        XmlDocumentationEmitter.Emit(text, doc);
        var xml = XElement.Parse("<doc>" + string.Join('\n', text.ToString().Split('\n')
            .Where(line => line.StartsWith("///", StringComparison.Ordinal)).Select(line => line[3..])) + "</doc>");
        Assert.Contains("a < b && b > c", xml.Element("param")!.Value);
        Assert.Contains("<value> & result", xml.Element("returns")!.Value);
        Assert.Empty(xml.Descendants("seealso"));
        Assert.Contains("See also: vtkMissing::Unknown()", xml.Element("remarks")!.Value);
    }

    [Theory]
    [InlineData(null, "fixed", 3, null)]
    [InlineData("in", null, null, null)]
    [InlineData("invalid", "fixed", 3, null)]
    [InlineData("in", "fixed", 0, null)]
    [InlineData("in", "fixed", -1, null)]
    [InlineData("in", "parameter", null, "missing")]
    [InlineData("in", "parameter", null, "values")]
    [InlineData("in", "parameter", null, "notInteger")]
    public void Validate_RejectsUnreliablePointerMetadata(string? direction, string? kind, int? value, string? name)
    {
        var function = Function("void", new() { Type = "double*", Name = "values", Direction = direction,
            Length = kind is null ? null : new() { Kind = kind, Value = value, Name = name } }, new() { Type = "double", Name = "notInteger" });
        var document = new WhitelistDocument { Classes = [new() { Name = "vtkThing", Functions = [function] }] };
        var inspected = new InspectedClass("vtkThing", [new("Method", "", "void", function.Parameters.Select(p => new InspectedParameter(p.Type, p.Name)).ToList(), true)]);
        Assert.NotEmpty(new WhitelistValidator().Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected }).Diagnostics);
    }

    private static ApiDocumentation Parse(string text)
    {
        var source = "/**\n" + text + "\n*/\nvoid Method();";
        return VtkDocumentationExtractor.Parse(source).GetDeclarationDocumentation(Encoding.UTF8.GetByteCount(source.AsSpan(0, source.IndexOf("void Method", StringComparison.Ordinal))))!;
    }

    private static WhitelistFunction Function(string returnType, params WhitelistParameter[] parameters)
        => new() { Name = "Method", Return = new() { Type = returnType }, Parameters = parameters.ToList() };

    [Theory]
    [InlineData("vtkThing*", "owned", true)]
    [InlineData("vtkThing*", "borrowed", true)]
    [InlineData("vtkThing*", "typo", false)]
    [InlineData("double*", "owned", false)]
    [InlineData("int", "owned", false)]
    public void Validate_RejectsUnsupportedOwnershipMetadata(string type, string ownership, bool valid)
    {
        var function = Function(type) with { Return = new() { Type = type, Ownership = ownership } };
        var document = new WhitelistDocument { Classes = [new() { Name = "vtkThing", Functions = [function] }] };
        var inspected = new InspectedClass("vtkThing", [new("Method", "", type, [], true)]);
        var result = new WhitelistValidator().Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected });
        Assert.Equal(valid, result.Diagnostics.Count == 0);
    }
}
