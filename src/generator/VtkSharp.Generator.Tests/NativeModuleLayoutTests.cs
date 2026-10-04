using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class NativeModuleLayoutTests
{
    [TestMethod]
    public void UnlistedModule_UsesIndependentTarget()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var layout = CreateLayout(sourceRoot.Path, Strategy(), ["vtkCommonCore", "vtkFiltersSources"]);

        Assert.AreEqual("VtkSharp.Native.CommonCore.dll", layout.GetNativeLibraryName("vtkCommonCore"));
        Assert.AreEqual("VtkSharp.Native.FiltersSources.dll", layout.GetNativeLibraryName("vtkFiltersSources"));
    }

    [TestMethod]
    public void GroupedModules_UseConfiguredTarget()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var strategy = Strategy(Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingCore", "vtkRenderingOpenGL2"], ["vtkRenderingOpenGL2"]));
        var layout = CreateLayout(sourceRoot.Path, strategy, ["vtkRenderingCore", "vtkRenderingOpenGL2"]);

        Assert.AreEqual("VtkSharp.Native.Rendering.dll", layout.GetNativeLibraryName("vtkRenderingCore"));
        Assert.AreEqual("VtkSharp.Native.Rendering.dll", layout.GetNativeLibraryName("vtkRenderingOpenGL2"));
        Assert.AreEqual(1, layout.Targets.Count);
        CollectionAssert.AreEqual(new[] { "vtkRenderingCore", "vtkRenderingOpenGL2" }, layout.Targets[0].Modules.ToArray());
    }

    [TestMethod]
    public void CSharpEmitter_UsesModuleLibraryNameAndEnsuresNetFrameworkPreload()
    {
        var text = new CSharpBindingEmitter().Emit(
            "VtkSharp", "vtkRenderWindow", "vtkObject", true, [],
            nativeLibraryName: "VtkSharp.Native.Rendering.dll");

        Assert.Contains("static vtkRenderWindow() => NativeModuleLoader.EnsureLoaded(\"VtkSharp.Native.Rendering.dll\");", text);
        Assert.Contains("[DllImport(\"VtkSharp.Native.Rendering.dll\")]", text);
    }

    [TestMethod]
    public void Create_UnknownWrappedModuleInGroup_Throws()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var strategy = Strategy(Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingCore", "vtkMissing"]));

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            CreateLayout(sourceRoot.Path, strategy, ["vtkRenderingCore"]));

        StringAssert.Contains(exception.Message, "unknown wrapped module 'vtkMissing'");
    }

    [TestMethod]
    public void Create_ModuleAssignedToMultipleGroups_Throws()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var strategy = Strategy(
            Group("First", "VtkSharp.Native.First", ["vtkRenderingCore"]),
            Group("Second", "VtkSharp.Native.Second", ["vtkRenderingCore"]));

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            CreateLayout(sourceRoot.Path, strategy, ["vtkRenderingCore"]));

        StringAssert.Contains(exception.Message, "assigned to more than one native target");
    }

    [TestMethod]
    public void Create_DuplicateGroupNameOrTarget_Throws()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var duplicateName = Strategy(
            Group("Rendering", "VtkSharp.Native.First", ["vtkRenderingCore"]),
            Group("Rendering", "VtkSharp.Native.Second", ["vtkRenderingOpenGL2"]));
        var duplicateTarget = Strategy(
            Group("First", "VtkSharp.Native.Rendering", ["vtkRenderingCore"]),
            Group("Second", "vtksharp.native.rendering", ["vtkRenderingOpenGL2"]));

        Assert.ThrowsExactly<InvalidDataException>(() => CreateLayout(sourceRoot.Path, duplicateName, ["vtkRenderingCore", "vtkRenderingOpenGL2"]));
        Assert.ThrowsExactly<InvalidDataException>(() => CreateLayout(sourceRoot.Path, duplicateTarget, ["vtkRenderingCore", "vtkRenderingOpenGL2"]));
    }

    [TestMethod]
    public void Create_InitializationProviderOutsideGroup_Throws()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var strategy = Strategy(Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingCore"], ["vtkRenderingOpenGL2"]));

        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            CreateLayout(sourceRoot.Path, strategy, ["vtkRenderingCore", "vtkRenderingOpenGL2"]));

        StringAssert.Contains(exception.Message, "is not a member of native module group");
    }

    [TestMethod]
    public void Create_MetadataValidatesFactoryProviderCoverage()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var group = Group("Rendering", "VtkSharp.Native.Rendering",
            ["vtkInteractionStyle", "vtkRenderingCore", "vtkRenderingLabel", "vtkRenderingOpenGL2", "vtkRenderingUI"],
            ["vtkInteractionStyle", "vtkRenderingOpenGL2", "vtkRenderingUI"]);
        var modules = new[]
        {
            new NativeModuleMetadata { Module = "vtkRenderingCore", Implementable = true, LibraryName = "vtkRenderingCore" },
            new NativeModuleMetadata { Module = "vtkRenderingLabel", Implementable = true, LibraryName = "vtkRenderingLabel" },
            new NativeModuleMetadata { Module = "vtkRenderingOpenGL2", Implementable = true, NeedsAutoinit = true, LibraryName = "vtkRenderingOpenGL2", Implements = ["VTK::RenderingCore", "VTK::RenderingLabel"] },
            new NativeModuleMetadata { Module = "vtkInteractionStyle", NeedsAutoinit = true, LibraryName = "vtkInteractionStyle", Implements = ["VTK::RenderingCore"] },
            new NativeModuleMetadata { Module = "vtkRenderingUI", NeedsAutoinit = true, LibraryName = "vtkRenderingUI", Implements = ["VTK::RenderingCore"] },
        };
        var metadata = new NativeModuleMetadataSnapshot
        {
            SchemaVersion = 1,
            VtkVersion = "9.7.0",
            Platform = "windows-x64",
            Configuration = "Release",
            Modules = modules.ToList(),
        };
        var strategy = Strategy(group);
        var wrappedModules = modules.Select(module => module.Module).ToArray();

        NativeModuleLayout.Create(strategy, wrappedModules, "VtkSharp.Native", "9.7", sourceRoot.Path, metadata);

        var invalidMetadata = metadata with
        {
            Modules = modules.Select(module => module.Module == "vtkRenderingOpenGL2"
                ? module with { Implements = ["VTK::RenderingCore"] }
                : module).ToList(),
        };
        var exception = Assert.ThrowsExactly<InvalidDataException>(() =>
            NativeModuleLayout.Create(strategy, wrappedModules, "VtkSharp.Native", "9.7", sourceRoot.Path, invalidMetadata));
        StringAssert.Contains(exception.Message, "do not implement required target 'vtkRenderingLabel'");
    }

    [TestMethod]
    public void Create_ManualSourceOwnershipMustResolveToOneExistingTarget()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var unknownTarget = Strategy() with
        {
            ManualSourceOwnership = [Source("vtksharp_string.cpp", "VtkSharp.Native.Missing")],
        };
        var missingFile = Strategy() with
        {
            ManualSourceOwnership = [Source("missing.cpp", "VtkSharp.Native.CommonCore")],
        };
        var duplicate = Strategy() with
        {
            ManualSourceOwnership =
            [
                Source("vtksharp_string.cpp", "VtkSharp.Native.CommonCore"),
                Source("vtksharp_string.cpp", "VtkSharp.Native.CommonCore"),
            ],
        };

        Assert.ThrowsExactly<InvalidDataException>(() => CreateLayout(sourceRoot.Path, unknownTarget, ["vtkCommonCore"]));
        Assert.ThrowsExactly<InvalidDataException>(() => CreateLayout(sourceRoot.Path, missingFile, ["vtkCommonCore"]));
        Assert.ThrowsExactly<InvalidDataException>(() => CreateLayout(sourceRoot.Path, duplicate, ["vtkCommonCore"]));
    }

    [TestMethod]
    public void Emit_GroupsModulesAndInitializationPerTarget()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var strategy = Strategy(
            Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingOpenGL2", "vtkRenderingCore"], ["vtkRenderingOpenGL2"])) with
        {
            ManualSourceOwnership = [Source("vtksharp_string.cpp", "VtkSharp.Native.CommonCore")],
        };
        var modules = new[] { "vtkCommonCore", "vtkRenderingCore", "vtkRenderingOpenGL2" };
        var layout = CreateLayout(sourceRoot.Path, strategy, modules);
        var documents = Documents(modules);

        var text = new CMakeModulesEmitter().Emit(layout, documents);

        Assert.Contains("set(VTKSHARP_NATIVE_TARGET_VtkSharp_Native_Rendering_MODULES", text);
        Assert.Contains("  RenderingCore", GetTargetBlock(text, "VtkSharp_Native_Rendering_MODULES"));
        Assert.Contains("  RenderingOpenGL2", GetTargetBlock(text, "VtkSharp_Native_Rendering_MODULES"));
        Assert.Contains("  RenderingOpenGL2", GetTargetBlock(text, "VtkSharp_Native_Rendering_AUTOINIT_MODULES"));
        Assert.Contains("src/vtksharp_string.cpp", GetTargetBlock(text, "VtkSharp_Native_CommonCore_SOURCES"));
        Assert.Contains("VtkSharp.Native.CommonCore", text);
        Assert.Contains("VtkSharp.Native.Rendering", text);
    }

    [TestMethod]
    public void Emit_ExcludesClassesOwnedByManualBindings()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var documents = new[]
        {
            new WhitelistDocument
            {
                Module = "vtkCommonCore",
                Classes =
                [
                    new WhitelistClass { Name = "vtkObjectBase", Header = "vtkObjectBase.h" },
                    new WhitelistClass { Name = "vtkGenerated", Header = "vtkGenerated.h" },
                ],
            },
        };
        var layout = CreateLayout(sourceRoot.Path, Strategy(), ["vtkCommonCore"]);

        var text = new CMakeModulesEmitter().Emit(layout, documents, new HashSet<string>(["vtkObjectBase"], StringComparer.Ordinal));

        Assert.DoesNotContain("vtkObjectBase_export_gen.cpp", text);
        Assert.Contains("vtkGenerated_export_gen.cpp", text);
    }

    [TestMethod]
    public void Emit_EquivalentStrategies_IsByteIdentical()
    {
        using var sourceRoot = new TemporarySourceRoot();
        var firstStrategy = Strategy(
            Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingCore", "vtkRenderingOpenGL2"], ["vtkRenderingOpenGL2"]));
        var reorderedStrategy = Strategy(
            Group("Rendering", "VtkSharp.Native.Rendering", ["vtkRenderingOpenGL2", "vtkRenderingCore"], ["vtkRenderingOpenGL2"]));
        var firstDocuments = Documents(["vtkCommonCore", "vtkRenderingCore", "vtkRenderingOpenGL2"]);
        var secondDocuments = firstDocuments.Reverse().ToArray();
        var emitter = new CMakeModulesEmitter();
        var first = emitter.Emit(CreateLayout(sourceRoot.Path, firstStrategy, firstDocuments.Select(document => document.Module)), firstDocuments);
        var second = emitter.Emit(CreateLayout(sourceRoot.Path, reorderedStrategy, secondDocuments.Select(document => document.Module)), secondDocuments);

        Assert.AreEqual(first, second);
        Assert.IsFalse(first.EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal));
    }

    private static NativeModuleLayout CreateLayout(string sourceRoot, NativeModuleStrategyConfig strategy, IEnumerable<string> modules)
        => NativeModuleLayout.Create(strategy, modules, "VtkSharp.Native", "9.7", sourceRoot);

    private static NativeModuleStrategyConfig Strategy(params NativeModuleGroupConfig[] groups)
        => new()
        {
            SchemaVersion = 1,
            StrategyVersion = 1,
            VtkVersion = "9.7.0",
            Platform = "windows-x64",
            Groups = groups.ToList(),
        };

    private static NativeModuleGroupConfig Group(string name, string target, List<string> modules, List<string>? providers = null)
        => new()
        {
            Name = name,
            Target = target,
            Modules = modules,
            InitializationProviders = providers ?? [],
            Rationale = "test",
        };

    private static ManualNativeSourceOwnershipConfig Source(string source, string target)
        => new() { Source = source, Target = target, Rationale = "test" };

    private static WhitelistDocument[] Documents(IEnumerable<string> modules)
        => modules.Select(module => new WhitelistDocument
        {
            Module = module,
            Classes = [new WhitelistClass { Name = $"{module}Example", Header = "example.h" }],
        }).ToArray();

    private static string GetTargetBlock(string text, string suffix)
    {
        var start = text.IndexOf(suffix, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0);
        var end = text.IndexOf(")", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start);
        return text[start..end];
    }

    private sealed class TemporarySourceRoot : IDisposable
    {
        public TemporarySourceRoot()
        {
            this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VtkSharp.NativeModuleTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.Path);
            File.WriteAllText(System.IO.Path.Combine(this.Path, "vtksharp_string.cpp"), "// test source");
        }

        public string Path { get; }

        public void Dispose()
            => Directory.Delete(this.Path, recursive: true);
    }
}
