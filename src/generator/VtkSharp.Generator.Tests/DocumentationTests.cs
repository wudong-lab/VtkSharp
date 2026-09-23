using System.Text;
using System.Xml.Linq;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class DocumentationTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("VtkSharp.Documentation.").FullName;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
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

        Assert.AreEqual(new ApiDocumentation("A thing & its geometry.", "Longer description <with> details.\n\nAnother paragraph."), inspected.Documentation);
        Assert.AreEqual(raw.Documentation, inspected.Documentation);
        Assert.AreEqual("Creates a thing.", inspected.NewDocumentation?.Summary);
        foreach (var name in new[] { "SetEnabled", "GetEnabled", "EnabledOn", "EnabledOff" })
        {
            var documentation = inspected.Functions.Single(f => f.Name == name).Documentation;
            Assert.AreEqual(new ApiDocumentation("Enable or disable the feature.", "Enabled by default."), documentation);
        }
        Assert.AreEqual(2, inspected.Functions.Count(f => f.Name == "SetPosition"));
        foreach (var f in inspected.Functions.Where(f => f.Name == "SetPosition")) Assert.AreEqual("Set vector coordinates.", f.Documentation?.Summary);
        Assert.IsNull(inspected.Functions.Single(f => f.Name == "Update").Documentation);
        Assert.IsNull(inspected.Functions.Single(f => f.Name == "Undocumented").Documentation);
        Assert.AreEqual("Local render.", inspected.Functions.Single(f => f.Name == "Render").Documentation?.Summary);

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
        Assert.Contains(element => element.Value.Contains("A thing & its geometry.", StringComparison.Ordinal), xml.Descendants("summary"));
    }

    [TestMethod]
    public void Inspect_MapsStructuredMacroAndOverloadCommentsToManagedParameters()
    {
        File.WriteAllText(Path.Combine(this._directory, "vtkThing.h"), """
            #define ValueMacro() void SetValue(double value); double GetValue();
            class vtkThing {
            public:
                ///@{
                /** Value access.
                 * @param value The scalar value.
                 * @return The current value.
                 */
                ValueMacro();
                ///@}
                /** Integer value.
                 * @param value Integer coordinate.
                 */
                void SetValue(int value);
                /** Vector value.
                 * @param values Coordinates.
                 */
                void SetVector(const double values[3]);
            };
            """);
        var inspected = new VtkClassInspector().InspectHeader(this._directory, "vtkThing.h", "vtkThing");
        var functions = inspected.Functions.Select(f => new WhitelistFunction
        {
            Name = f.Name, Return = new() { Type = f.ReturnType },
            Parameters = f.Parameters.Select(p => new WhitelistParameter { Type = p.Type, Name = "renamed" }).ToList(),
        }).ToList();
        foreach (var function in functions)
        {
            var source = inspected.Functions.Single(f => f.Name == function.Name &&
                f.Parameters.Select(p => p.Type).SequenceEqual(function.Parameters.Select(p => p.Type)));
            var doc = BindingDocumentation.ForMethod(function, source);
            if (function.Name == "GetValue")
            {
                Assert.IsNull(doc.Parameters);
                Assert.AreEqual("The current value.", doc.Returns);
            }
            else
            {
                Assert.IsNull(doc.Returns);
                Assert.AreEqual("renamed", Enumerable.Single(doc.Parameters!).Name);
                if (function.Name == "SetVector") Assert.Contains("3 elements", doc.Parameters![0].Text);
                else Assert.Contains(function.Parameters[0].Type == "int" ? "Integer coordinate." : "The scalar value.", doc.Parameters![0].Text);
            }
        }
    }

    [TestMethod]
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
        Assert.IsNull(Find("void Empty"));
        Assert.AreEqual("First.", Find("void First")?.Summary);
        Assert.IsNull(Find("void Second"));
        Assert.IsNull(Find("void Third"));
        Assert.IsNull(Find("void Fourth"));
        Assert.AreEqual(new ApiDocumentation("Outer.", "More details."), Find("void Fifth"));
        Assert.AreEqual("Sixth.", Find("void Sixth")?.Summary);
        Assert.IsNull(extractor.GetClassDocumentation("vtkFake", 0));
    }

    [TestMethod]
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
        Assert.AreEqual("Outer.", Find("void First")?.Summary);
        Assert.AreEqual("Inner.", Find("void Second")?.Summary);
        Assert.AreEqual("Outer.", Find("void Third")?.Summary);
        Assert.IsNull(Find("void Fourth"));
        Assert.IsNull(Find("void Fifth"));
    }

    [TestMethod]
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
        Assert.AreEqual("Shared.", Find("void First")?.Summary);
        Assert.AreEqual("Separate.", Find("void Second")?.Summary);
        Assert.IsNull(Find("void Third"));
        Assert.AreEqual("Fourth.", Find("void Fourth")?.Summary);
    }

    [TestMethod]
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
        Assert.AreEqual("First.", extractor.GetDeclarationDocumentation(source.IndexOf("void First", StringComparison.Ordinal))?.Summary);
        Assert.IsNull(extractor.GetDeclarationDocumentation(source.IndexOf("void Second", StringComparison.Ordinal)));
    }

    [TestMethod]
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
                    cppSignature: int Update(int value)
                    return:
                      type: int
                    parameters:
                      - type: int
                        name: renamed
            """);
        var header = Path.Combine(this._directory, "vtkThing.h");
        File.WriteAllText(header, "class vtkThing { public: /** Original.\n@param value Original parameter.\n@return Result.\n*/ int Update(int value); };");
        var full = Path.Combine(this._directory, "full");
        var incremental = Path.Combine(this._directory, "incremental");
        var generator = new BindingGenerationService();
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.AreEqual(0, generator.Generate(config, full, false, false, false, output, error));
        Assert.AreEqual(0, generator.Generate(config, incremental, false, true, false, output, error));
        const string relative = "bindings/VtkSharp/vtkCommonCore/vtkThing_gen.cs";
        Assert.AreEqual(File.ReadAllText(Path.Combine(full, relative)), File.ReadAllText(Path.Combine(incremental, relative)));
        Assert.Contains("/// Original.", File.ReadAllText(Path.Combine(full, relative)));
        Assert.Contains("<param name=\"renamed\">", File.ReadAllText(Path.Combine(full, relative)));
        Assert.Contains("<returns>", File.ReadAllText(Path.Combine(full, relative)));
        output.GetStringBuilder().Clear();
        Assert.AreEqual(0, generator.Generate(config, incremental, false, true, false, output, error));
        Assert.Contains("generated 0 class(es), reused 1 class(es)", output.ToString());
        File.WriteAllText(header, "class vtkThing { public: /** Original.\n@param value Revised.\n@return Result.\n*/ int Update(int value); };");
        Assert.AreEqual(0, generator.Generate(config, incremental, false, true, false, output, error));
        Assert.Contains("/// Revised.", File.ReadAllText(Path.Combine(incremental, relative)));
        Assert.AreEqual("", error.ToString());
    }

    [TestMethod]
    public void CheckGeneratedOutputIncremental_ReusesValidEntriesAndDetectsEditedOrUnexpectedFiles()
    {
        var config = Path.Combine(this._directory, "incremental-check.yml");
        var whitelist = Path.Combine(this._directory, "incremental-check-whitelist");
        var outputRoot = Path.Combine(this._directory, "incremental-check-output");
        Directory.CreateDirectory(whitelist);
        File.WriteAllText(config, $$"""
            vtk:
              version: "9.7"
              includeDirectory: '{{this._directory}}'
            binding:
              namespace: VtkSharp
              nativeLibraryName: VtkSharp.Native
            paths:
              whitelistDirectory: incremental-check-whitelist
              managedOutputDirectory: incremental-check-output/bindings/VtkSharp
              nativeOutputDirectory: incremental-check-output/bindings/VtkSharp.Native/src
              nativeProjectFile: incremental-check-output/bindings/VtkSharp.Native/CMakeLists.txt
              nativeModulesFile: incremental-check-output/bindings/VtkSharp.Native/vtksharp.modules.generated.cmake
            """);
        File.WriteAllText(Path.Combine(whitelist, "vtkCommonCore.yml"), """
            module: vtkCommonCore
            classes:
              - name: vtkThing
                header: vtkThing.h
                functions: []
            """);
        File.WriteAllText(Path.Combine(this._directory, "vtkThing.h"),
            "class vtkThing { public: static vtkThing* New(); };");

        var generator = new BindingGenerationService();
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.AreEqual(0, generator.Generate(config, outputRoot, false, true, false, output, error));

        output.GetStringBuilder().Clear();
        Assert.AreEqual(0, generator.CheckGeneratedOutputIncremental(config, output, error));
        Assert.Contains("reused 1 class(es) and inspected 0 class(es)", output.ToString());

        var managedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", "vtkCommonCore", "vtkThing_gen.cs");
        File.AppendAllText(managedPath, "// manual edit");
        error.GetStringBuilder().Clear();
        Assert.AreEqual(1, generator.CheckGeneratedOutputIncremental(config, output, error));
        Assert.Contains("vtkThing_gen.cs: Content differs.", error.ToString());

        Assert.AreEqual(0, generator.Generate(config, outputRoot, false, true, false, output, error));
        var unexpectedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", "vtkCommonCore", "vtkUnexpected_gen.cs");
        File.WriteAllText(unexpectedPath, "// unexpected");
        error.GetStringBuilder().Clear();
        Assert.AreEqual(1, generator.CheckGeneratedOutputIncremental(config, output, error));
        Assert.Contains("vtkUnexpected_gen.cs: Only exists in current output.", error.ToString());
    }

    [TestMethod]
    public void XmlEmitter_EscapesMarkupAndPreservesUnimplementedCommandsAsText()
    {
        var output = new StringBuilder();
        XmlDocumentationEmitter.Emit(output, new ApiDocumentation("a < b && b > c", "@param x <value>\n@return &result;\n\n@code\n<tag>\n@endcode"));
        var xml = XElement.Parse("<doc>" + string.Join('\n', output.ToString().Split('\n')
            .Where(line => line.StartsWith("///", StringComparison.Ordinal)).Select(line => line[3..])) + "</doc>");
        Assert.AreEqual("\n a < b && b > c\n ", xml.Element("summary")!.Value);
        Assert.IsEmpty(xml.Descendants("param"));
        Assert.AreEqual(2, xml.Descendants("para").Count());
        Assert.Contains("@param x <value>", xml.Element("remarks")!.Value);
    }

    public void Dispose() => Directory.Delete(this._directory, recursive: true);
}
