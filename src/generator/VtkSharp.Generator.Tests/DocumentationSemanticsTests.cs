using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class DocumentationSemanticsTests
{
    [TestMethod]
    [DataRow("FromBorrowedPointer", "nativePointer", "without adding a reference", "does not release the borrowed reference")]
    [DataRow("TakeReference", "nativePointer", "without incrementing the reference count", "do not release it separately or transfer it twice")]
    [DataRow("Register", "sourceObject", "increments its reference count by one", "source wrapper's ownership is unchanged")]
    public void Emit_StaticHelpersDocumentOwnershipAndRelease(string method, string parameter, string summary, string remarks)
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkObject", true, []);
        var match = Regex.Match(text, @"(?<doc>(?:    ///[^\r\n]*\r?\n)+)    public new static vtkThing " + method + @"\(");
        Assert.IsTrue(match.Success);
        var xml = XElement.Parse("<doc>" + string.Join('\n', match.Groups["doc"].Value.Split('\n')
            .Where(line => line.TrimStart().StartsWith("///", StringComparison.Ordinal)).Select(line => line.TrimStart()[3..])) + "</doc>");
        Assert.Contains(summary, xml.Element("summary")!.Value);
        Assert.Contains(remarks, xml.Element("remarks")!.Value);
        Assert.AreEqual(parameter, xml.Element("param")!.Attribute("name")!.Value);
        Assert.IsNotNull(xml.Element("returns"));
        Assert.Contains("Dispose()", xml.Element("remarks")!.Value);
    }

    [TestMethod]
    [DataRow("@")]
    [DataRow("\\")]
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
        Assert.AreEqual("Compute values.", doc.Summary);
        Assert.AreEqual(2, doc.Parameters!.Count);
        Assert.AreEqual("in", doc.Parameters[0].Direction);
        Assert.Contains("second paragraph", doc.Parameters[0].Text);
        Assert.Contains("special <values>", doc.Parameters[0].Text);
        Assert.AreEqual("out", doc.Parameters[1].Direction);
        Assert.AreEqual("An array of 3 elements owned by this object.", doc.Returns);
        Assert.Contains("See also: vtkMissing::Overloaded(int) vtkOther", doc.Remarks);
        Assert.Contains("Warning: The pointer is invalidated", doc.Remarks);
        Assert.DoesNotContain("Discard", doc.Remarks);
        Assert.DoesNotContain("fake", doc.ToString());
    }

    [TestMethod]
    [DataRow("@verbatim", "@endverbatim")]
    [DataRow("@code{.cpp}", "@endcode")]
    [DataRow("```cpp", "```")]
    [DataRow("~~~cpp", "~~~")]
    public void Parse_SkippedBlocksDoNotSwallowFollowingReturn(string start, string end)
    {
        var doc = Parse($"Summary.\n{start}\n@param fake Invalid\n{end}\n@return Useful result.");
        Assert.IsNull(doc.Parameters);
        Assert.AreEqual("Useful result.", doc.Returns);
    }

    [TestMethod]
    public void Parse_ParameterListsDirectionsAndMultilineSeeAlso()
    {
        var doc = Parse("@param[in,out] x,y Coordinates.\n@sa\nvtkOne vtkTwo\n@retval 0 No result.\n@retval 1 Success.");
        Assert.IsNull(doc.Summary);
        Assert.AreSequenceEqual(["x", "y"], doc.Parameters!.Select(p => p.Name));
        foreach (var p in doc.Parameters!) Assert.AreEqual("in,out", p.Direction);
        Assert.AreEqual("See also: vtkOne vtkTwo", doc.Remarks);
        Assert.AreEqual("0 No result.\n\n1 Success.", doc.Returns);
    }

    [TestMethod]
    public void Map_RenamesParametersAndFiltersSharedGroupDocumentation()
    {
        var source = Parse("Set or get values.\n@param x Coordinate.\n@param other Other overload.\n@return Current value.");
        var function = Function("void", new WhitelistParameter { Type = "double", Name = "event" }, new WhitelistParameter { Type = "int", Name = "count" });
        var inspected = new InspectedFunction("Method", "", "void", [new("double", "x"), new("int", "n")], true, Documentation: source);
        var doc = BindingDocumentation.ForMethod(function, inspected);
        Assert.IsNull(doc.Returns);
        Assert.AreSequenceEqual(["event", "count"], doc.Parameters!.Select(p => p.Name));
        Assert.AreEqual("Coordinate.", doc.Parameters![0].Text);
        Assert.AreEqual("", doc.Parameters![1].Text);
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkBase", false, [function], new("vtkThing", [inspected]));
        Assert.Contains("double @event", text);
        Assert.Contains("<param name=\"event\">", text);
        Assert.Contains("<param name=\"count\" />", text);
        Assert.DoesNotContain("Other overload.", text);
    }

    [TestMethod]
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
        Assert.IsEmpty(doc.Parameters[3].Text);
    }

    [TestMethod]
    [DataRow(null, "borrows the native object")]
    [DataRow("borrowed", "borrows the native object")]
    [DataRow("owned", "owns a native reference")]
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

    [TestMethod]
    [DataRow("const char*", "managed string")]
    [DataRow("vtkStdString", "managed string")]
    [DataRow("vtkColor3d", "C# value type")]
    public void Map_CopiedReturnsDoNotTransferNativeMemoryToCaller(string type, string description)
    {
        var doc = BindingDocumentation.ForMethod(Function(type), null);
        Assert.Contains(description, doc.Remarks);
        Assert.Contains("does not release native memory", doc.Remarks);
    }

    [TestMethod]
    public void Map_ReturnPointerDoesNotInventLengthOrOwnership()
    {
        var function = Function("double*");
        var source = new InspectedFunction("Method", "", "double*", [], true, Documentation: Parse("@return The result buffer."));
        var doc = BindingDocumentation.ForMethod(function, source);
        Assert.AreEqual("The result buffer.", doc.Returns);
        Assert.IsNull(doc.Remarks);
        source = source with { Documentation = Parse("@return A buffer containing 3 elements; valid until the next call.") };
        Assert.AreEqual(source.Documentation.Returns, BindingDocumentation.ForMethod(function, source).Returns);
    }

    [TestMethod]
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

    [TestMethod]
    public void Emit_EscapesStructuredDocumentationAndUsesPlainSeeAlso()
    {
        var doc = Parse("Summary.\n@param x a < b && b > c\n@return <value> & result\n@sa vtkMissing::Unknown()");
        var text = new StringBuilder();
        XmlDocumentationEmitter.Emit(text, doc);
        var xml = XElement.Parse("<doc>" + string.Join('\n', text.ToString().Split('\n')
            .Where(line => line.StartsWith("///", StringComparison.Ordinal)).Select(line => line[3..])) + "</doc>");
        Assert.Contains("a < b && b > c", xml.Element("param")!.Value);
        Assert.Contains("<value> & result", xml.Element("returns")!.Value);
        Assert.IsEmpty(xml.Descendants("seealso"));
        Assert.Contains("See also: vtkMissing::Unknown()", xml.Element("remarks")!.Value);
    }

    [TestMethod]
    [DataRow(null, "fixed", 3, null)]
    [DataRow("in", null, null, null)]
    [DataRow("invalid", "fixed", 3, null)]
    [DataRow("in", "fixed", 0, null)]
    [DataRow("in", "fixed", -1, null)]
    [DataRow("in", "parameter", null, "missing")]
    [DataRow("in", "parameter", null, "values")]
    [DataRow("in", "parameter", null, "notInteger")]
    public void Validate_RejectsUnreliablePointerMetadata(string? direction, string? kind, int? value, string? name)
    {
        var function = Function("void", new() { Type = "double*", Name = "values", Direction = direction,
            Length = kind is null ? null : new() { Kind = kind, Value = value, Name = name } }, new() { Type = "double", Name = "notInteger" });
        var document = new WhitelistDocument { Classes = [new() { Name = "vtkThing", Functions = [function] }] };
        var inspected = new InspectedClass("vtkThing", [new("Method", "", "void", function.Parameters.Select(p => new InspectedParameter(p.Type, p.Name)).ToList(), true)]);
        Assert.IsNotEmpty(new WhitelistValidator().Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected }).Diagnostics);
    }

    private static ApiDocumentation Parse(string text)
    {
        var source = "/**\n" + text + "\n*/\nvoid Method();";
        return VtkDocumentationExtractor.Parse(source).GetDeclarationDocumentation(Encoding.UTF8.GetByteCount(source.AsSpan(0, source.IndexOf("void Method", StringComparison.Ordinal))))!;
    }

    private static WhitelistFunction Function(string returnType, params WhitelistParameter[] parameters)
        => new() { Name = "Method", Return = new() { Type = returnType }, Parameters = parameters.ToList() };

    [TestMethod]
    [DataRow("vtkThing*", "owned", true)]
    [DataRow("vtkThing*", "borrowed", true)]
    [DataRow("vtkThing*", "typo", false)]
    [DataRow("double*", "owned", false)]
    [DataRow("int", "owned", false)]
    public void Validate_RejectsUnsupportedOwnershipMetadata(string type, string ownership, bool valid)
    {
        var function = Function(type) with { Return = new() { Type = type, Ownership = ownership } };
        var document = new WhitelistDocument { Classes = [new() { Name = "vtkThing", Functions = [function] }] };
        var inspected = new InspectedClass("vtkThing", [new("Method", "", type, [], true)]);
        var result = new WhitelistValidator().Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected });
        Assert.AreEqual(valid, result.Diagnostics.Count == 0);
    }
}
