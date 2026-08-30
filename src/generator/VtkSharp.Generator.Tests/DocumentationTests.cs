using System.Text;
using System.Xml.Linq;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class DocumentationTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("VtkSharp.Documentation.").FullName;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Inspect_PreservesClassMethodsGroupsMacrosAndLocalOverrideComments(bool bom)
    {
        var source = """
            /**
             * @class vtkThing
             * @brief A thing & its geometry.
             *
             * Longer description <with> details.
             *
             * Another paragraph.
             */
            #ifndef vtkThing_h
            #define vtkThing_h
            // 中文：验证 Clang 的 UTF-8 字节偏移。
            #define vtkSetMacro(name, type) void Set##name(type value) {}
            #define vtkGetMacro(name, type) type Get##name() { return {}; }
            #define vtkBooleanMacro(name, type) void name##On() {} void name##Off() {}
            #define vtkVectorMacro(name) void Set##name(double x, double y) {} void Set##name(const double v[2]) {}
            class vtkBase {
            public:
                /** Base description, not to be inherited. */
                virtual void Update() {}
                /** Base render. */
                virtual void Render() {}
            };
            class vtkThing : public vtkBase
            {
            public:
                /** Creates a thing. */
                static vtkThing* New();
                ///@{
                /**
                 * Enable or disable the feature.
                 *
                 * Enabled by default.
                 */
                vtkSetMacro(Enabled, bool);
                vtkGetMacro(Enabled, bool);
                vtkBooleanMacro(Enabled, bool);
                ///@}
                ///@{
                /** Set vector coordinates. */
                vtkVectorMacro(Position);
                ///@}
                /** Set an integer. */
                void SetValue(int originalName);
                /** Set a floating-point value. */
                void SetValue(double originalName);
                void Update() override;
                /** Local render. */
                void Render() override;
                void Undocumented();
            };
            #endif
            """;
        var path = Path.Combine(this._directory, "vtkThing.h");
        File.WriteAllText(path, source.ReplaceLineEndings("\r\n"), new UTF8Encoding(bom));
        var inspector = new VtkClassInspector();
        var raw = inspector.InspectFile(this._directory, "vtkThing.h")["vtkThing"];
        var inspected = inspector.InspectHeader(this._directory, "vtkThing.h", "vtkThing");

        Assert.Equal(new ApiDocumentation("A thing & its geometry.", "Longer description <with> details.\n\nAnother paragraph."), inspected.Documentation);
        Assert.Equal(raw.Documentation, inspected.Documentation);
        Assert.Equal("Creates a thing.", inspected.NewDocumentation?.Summary);
        foreach (var name in new[] { "SetEnabled", "GetEnabled", "EnabledOn", "EnabledOff" })
        {
            var documentation = inspected.Functions.Single(f => f.Name == name).Documentation;
            Assert.Equal(new ApiDocumentation("Enable or disable the feature.", "Enabled by default."), documentation);
        }
        Assert.Equal(2, inspected.Functions.Count(f => f.Name == "SetPosition"));
        Assert.All(inspected.Functions.Where(f => f.Name == "SetPosition"), f => Assert.Equal("Set vector coordinates.", f.Documentation?.Summary));
        Assert.Null(inspected.Functions.Single(f => f.Name == "Update").Documentation);
        Assert.Null(inspected.Functions.Single(f => f.Name == "Undocumented").Documentation);
        Assert.Equal("Local render.", inspected.Functions.Single(f => f.Name == "Render").Documentation?.Summary);

        var functions = new[] { "double", "int" }.Select(type => new WhitelistFunction
        {
            Name = "SetValue",
            Return = new WhitelistReturn { Type = "void" },
            Parameters = [new WhitelistParameter { Name = "renamed", Type = type }],
        }).ToList();
        var generated = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkBase", true, functions, inspected);
        Assert.Contains("/// A thing &amp; its geometry.", generated);
        Assert.Contains("/// Longer description &lt;with&gt; details.", generated);
        Assert.Contains("/// <para>", generated);
        Assert.Contains("/// Creates a thing.", generated);
        Assert.Contains("/// Set a floating-point value.\n    /// </summary>\n    public new void SetValue(double renamed)", generated.ReplaceLineEndings("\n"));
        Assert.Contains("/// Set an integer.\n    /// </summary>\n    public new void SetValue(int renamed)", generated.ReplaceLineEndings("\n"));
        Assert.DoesNotContain("///@", generated);
        var xml = XDocument.Parse("<doc>" + string.Join('\n', generated.Split('\n')
            .Where(line => line.TrimStart().StartsWith("///", StringComparison.Ordinal)).Select(line => line.TrimStart()[3..])) + "</doc>");
        Assert.Contains(xml.Descendants("summary"), element => element.Value.Contains("A thing & its geometry.", StringComparison.Ordinal));
    }

    [Fact]
    public void Extract_DoesNotLeakAcrossDeclarationsOrLexicalBoundaries()
    {
        const string source = """"
            #define FAKE /* misleading */ \
                /** @class vtkFake */
            const char* text = R"tag(/** @class vtkFake */ ///@{)tag";
            const char* quoted = "\"/** misleading */";
            const int count = 1'000;
            /**/ void Empty();
            /** First. */ void First(); void Second();
            int field; ///< Field documentation.
            void Third();
            /** @file Only file documentation. */
            void Fourth();
            /// Outer.
            ///
            /// More details.
            void Fifth();
            /** \brief Sixth. */
            void Sixth();
            """";
        var extractor = VtkDocumentationExtractor.Parse(source);
        ApiDocumentation? Find(string declaration) => extractor.GetDeclarationDocumentation(Encoding.UTF8.GetByteCount(source.AsSpan(0, source.IndexOf(declaration, StringComparison.Ordinal))));
        Assert.Null(Find("void Empty"));
        Assert.Equal("First.", Find("void First")?.Summary);
        Assert.Null(Find("void Second"));
        Assert.Null(Find("void Third"));
        Assert.Null(Find("void Fourth"));
        Assert.Equal(new ApiDocumentation("Outer.", "More details."), Find("void Fifth"));
        Assert.Equal("Sixth.", Find("void Sixth")?.Summary);
        Assert.Null(extractor.GetClassDocumentation("vtkFake", 0));
    }

    [Fact]
    public void Extract_NestedGroupsRestoreOuterDocumentation()
    {
        const string source = """
            class vtkThing {
            ///@{
            /** Outer. */
            void First();
            ///@{
            /** Inner. */
            void Second();
            ///@}
            void Third();
            ///@}
            void Fourth();
            };
            void Fifth();
            """;
        var extractor = VtkDocumentationExtractor.Parse(source);
        ApiDocumentation? Find(string declaration) => extractor.GetDeclarationDocumentation(source.IndexOf(declaration, StringComparison.Ordinal));
        Assert.Equal("Outer.", Find("void First")?.Summary);
        Assert.Equal("Inner.", Find("void Second")?.Summary);
        Assert.Equal("Outer.", Find("void Third")?.Summary);
        Assert.Null(Find("void Fourth"));
        Assert.Null(Find("void Fifth"));
    }

    [Fact]
    public void Extract_LineGroupsAndPreprocessorBranchesDoNotShareStaleComments()
    {
        const string source = """
            class vtkThing {
            ///@{
            /// Shared.
            void First();
            ///@}
            /// Separate.
            void Second();
            ///@{
            /*! Disabled branch. */
            #if 0
            void Disabled();
            #endif
            void Third();
            ///@}
            /*! Fourth. */
            void Fourth();
            };
            """;
        var extractor = VtkDocumentationExtractor.Parse(source);
        ApiDocumentation? Find(string declaration) => extractor.GetDeclarationDocumentation(source.IndexOf(declaration, StringComparison.Ordinal));
        Assert.Equal("Shared.", Find("void First")?.Summary);
        Assert.Equal("Separate.", Find("void Second")?.Summary);
        Assert.Null(Find("void Third"));
        Assert.Equal("Fourth.", Find("void Fourth")?.Summary);
    }

    [Fact]
    public void Extract_AlternativeDeclarationsCannotLeakIntoLaterMethods()
    {
        const string source = """
            #if USE_INT
            void Conditional(int value
            #else
            void Conditional(double value
            #endif
            );
            /** First. */ void First();
            void Second();
            """;
        var extractor = VtkDocumentationExtractor.Parse(source);
        Assert.Equal("First.", extractor.GetDeclarationDocumentation(source.IndexOf("void First", StringComparison.Ordinal))?.Summary);
        Assert.Null(extractor.GetDeclarationDocumentation(source.IndexOf("void Second", StringComparison.Ordinal)));
    }

    [Fact]
    public void Generate_FullAndIncrementalAgreeAndCommentOnlyChangesInvalidateCache()
    {
        var config = Path.Combine(this._directory, "config.yml");
        var whitelist = Path.Combine(this._directory, "whitelist");
        Directory.CreateDirectory(whitelist);
        File.WriteAllText(config, $$"""
            vtk:
              version: "9.7"
              includeDirectory: '{{this._directory}}'
            binding:
              namespace: VtkSharp
              nativeLibraryName: VtkSharp.Native
            paths:
              whitelistDirectory: whitelist
            """);
        File.WriteAllText(Path.Combine(whitelist, "vtkCommonCore.yml"), """
            module: vtkCommonCore
            classes:
              - name: vtkThing
                header: vtkThing.h
                functions:
                  - name: Update
                    cppSignature: void Update()
                    return:
                      type: void
                    parameters: []
            """);
        var header = Path.Combine(this._directory, "vtkThing.h");
        File.WriteAllText(header, "class vtkThing { public: /** Original. */ void Update(); };");
        var full = Path.Combine(this._directory, "full");
        var incremental = Path.Combine(this._directory, "incremental");
        var generator = new BindingGenerationService();
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.Equal(0, generator.Generate(config, full, false, false, false, output, error));
        Assert.Equal(0, generator.Generate(config, incremental, false, true, false, output, error));
        const string relative = "bindings/VtkSharp/vtkCommonCore/vtkThing_gen.cs";
        Assert.Equal(File.ReadAllText(Path.Combine(full, relative)), File.ReadAllText(Path.Combine(incremental, relative)));
        Assert.Contains("/// Original.", File.ReadAllText(Path.Combine(full, relative)));
        output.GetStringBuilder().Clear();
        Assert.Equal(0, generator.Generate(config, incremental, false, true, false, output, error));
        Assert.Contains("generated 0 class(es), reused 1 class(es)", output.ToString());
        File.WriteAllText(header, "class vtkThing { public: /** Revised. */ void Update(); };");
        Assert.Equal(0, generator.Generate(config, incremental, false, true, false, output, error));
        Assert.Contains("/// Revised.", File.ReadAllText(Path.Combine(incremental, relative)));
        Assert.Equal("", error.ToString());
    }

    [Fact]
    public void XmlEmitter_EscapesMarkupAndPreservesUnimplementedCommandsAsText()
    {
        var output = new StringBuilder();
        XmlDocumentationEmitter.Emit(output, new ApiDocumentation("a < b && b > c", "@param x <value>\n@return &result;\n\n@code\n<tag>\n@endcode"));
        var xml = XElement.Parse("<doc>" + string.Join('\n', output.ToString().Split('\n')
            .Where(line => line.StartsWith("///", StringComparison.Ordinal)).Select(line => line[3..])) + "</doc>");
        Assert.Equal("\n a < b && b > c\n ", xml.Element("summary")!.Value);
        Assert.Empty(xml.Descendants("param"));
        Assert.Equal(2, xml.Descendants("para").Count());
        Assert.Contains("@param x <value>", xml.Element("remarks")!.Value);
    }

    public void Dispose() => Directory.Delete(this._directory, recursive: true);
}
