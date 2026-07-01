using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class GeneratedManifestCacheTests
{
    [Fact]
    public void TryGetReusableEntry_ReturnsTrue_WhenInputAndOutputHashesMatch()
    {
        var directory = CreateDirectory();
        var managedPath = Path.Combine(directory, "vtkFoo_gen.cs");
        var nativePath = Path.Combine(directory, "vtkFoo_export_gen.cpp");
        File.WriteAllText(managedPath, "managed\n");
        File.WriteAllText(nativePath, "native\n");
        var manifest = CreateManifest("input", managedPath, nativePath);

        var reusable = GeneratedManifestCache.TryGetReusableEntry(manifest, "vtkFoo", "input", managedPath, nativePath, out var entry);

        Assert.True(reusable);
        Assert.Equal("vtkFoo", entry?.ClassName);
    }

    [Fact]
    public void TryGetReusableEntry_ReturnsFalse_WhenInputHashDiffers()
    {
        var directory = CreateDirectory();
        var managedPath = Path.Combine(directory, "vtkFoo_gen.cs");
        var nativePath = Path.Combine(directory, "vtkFoo_export_gen.cpp");
        File.WriteAllText(managedPath, "managed\n");
        File.WriteAllText(nativePath, "native\n");
        var manifest = CreateManifest("old", managedPath, nativePath);

        var reusable = GeneratedManifestCache.TryGetReusableEntry(manifest, "vtkFoo", "new", managedPath, nativePath, out _);

        Assert.False(reusable);
    }

    [Fact]
    public void TryGetReusableEntry_ReturnsFalse_WhenGeneratedFileWasEdited()
    {
        var directory = CreateDirectory();
        var managedPath = Path.Combine(directory, "vtkFoo_gen.cs");
        var nativePath = Path.Combine(directory, "vtkFoo_export_gen.cpp");
        File.WriteAllText(managedPath, "managed\n");
        File.WriteAllText(nativePath, "native\n");
        var manifest = CreateManifest("input", managedPath, nativePath);
        File.WriteAllText(managedPath, "manual edit\n");

        var reusable = GeneratedManifestCache.TryGetReusableEntry(manifest, "vtkFoo", "input", managedPath, nativePath, out _);

        Assert.False(reusable);
    }

    [Fact]
    public void ComputeFingerprint_Changes_WhenWhitelistFunctionChanges()
    {
        var first = GenerationInputFingerprint.Compute(
            "v1",
            "9.5",
            "VtkSharp",
            "VtkSharp.Native",
            "vtkCommonCore",
            "vtkFoo",
            "vtkFoo.h",
            "vtkObject",
            "header",
            [CreateFunction("SetValue", "int")]);
        var second = GenerationInputFingerprint.Compute(
            "v1",
            "9.5",
            "VtkSharp",
            "VtkSharp.Native",
            "vtkCommonCore",
            "vtkFoo",
            "vtkFoo.h",
            "vtkObject",
            "header",
            [CreateFunction("SetValue", "double")]);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Load_ReturnsEmptyManifest_WhenSchemaVersionDiffers()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, GeneratedManifestStore.FileName);
        File.WriteAllText(path, """
            {
              "schemaVersion": 999,
              "generatorVersion": "v1",
              "module": "vtkCommonCore",
              "classes": [
                { "className": "vtkFoo" }
              ]
            }
            """);

        var manifest = new GeneratedManifestStore().Load(path, "vtkCommonCore", "v1");

        Assert.Empty(manifest.Classes);
    }

    private static GeneratedManifest CreateManifest(string inputHash, string managedPath, string nativePath)
        => new()
        {
            SchemaVersion = GeneratedManifestStore.CurrentSchemaVersion,
            GeneratorVersion = "v1",
            Module = "vtkCommonCore",
            Classes =
            [
                new GeneratedManifestEntry
                {
                    ClassName = "vtkFoo",
                    Header = "vtkFoo.h",
                    BaseClassName = "vtkObject",
                    HasStaticNew = true,
                    InputHash = inputHash,
                    ManagedPath = "bindings/VtkSharp/vtkCommonCore/vtkFoo_gen.cs",
                    NativePath = "native/src/vtkCommonCore/vtkFoo_export_gen.cpp",
                    ManagedContentHash = GenerationInputFingerprint.HashFileText(managedPath),
                    NativeContentHash = GenerationInputFingerprint.HashFileText(nativePath),
                },
            ],
        };

    private static WhitelistFunction CreateFunction(string name, string parameterType)
        => new()
        {
            Name = name,
            CppSignature = $"void {name}({parameterType})",
            Return = new WhitelistReturn { Type = "void" },
            Parameters =
            [
                new WhitelistParameter { Type = parameterType, Name = "value" },
            ],
        };

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
