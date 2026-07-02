using VtkSharp.Generator.Core.Configuration;

namespace VtkSharp.Generator.Tests;

public sealed class GeneratorWorkspaceTests
{
    [Fact]
    public void Load_ResolvesWhitelistAndVtkDirectoriesFromConfig()
    {
        var root = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        var configDirectory = Path.Combine(root, "config");
        var whitelistDirectory = Path.Combine(root, "whitelist");
        var includeDirectory = Path.Combine(root, "vtk", "include");
        var hierarchyDirectory = Path.Combine(root, "vtk", "hierarchy");
        Directory.CreateDirectory(configDirectory);
        Directory.CreateDirectory(whitelistDirectory);
        Directory.CreateDirectory(includeDirectory);
        Directory.CreateDirectory(hierarchyDirectory);

        var configPath = Path.Combine(configDirectory, "vtksharp.generator.yml");
        File.WriteAllText(configPath, $$"""
            vtk:
              version: "9.5"
              modulePrefix: "VTK::"
              includeDirectory: "{{includeDirectory.Replace("\\", "\\\\", StringComparison.Ordinal)}}"
              hierarchyDirectory: "{{hierarchyDirectory.Replace("\\", "\\\\", StringComparison.Ordinal)}}"
            binding:
              namespace: "VtkSharp"
              nativeLibraryName: "vtksharp"
              manualBindingClasses: []
            paths:
              whitelistDirectory: "../whitelist"
              managedOutputDirectory: "../src/bindings"
              nativeOutputDirectory: "../src/native"
              nativeProjectFile: "../src/native/CMakeLists.txt"
              nativeModulesFile: "../src/native/vtksharp.modules.generated.cmake"
            generation:
              targetFramework: "net10.0"
            """);

        var workspace = GeneratorWorkspace.Load(configPath);

        Assert.Equal(Path.GetFullPath(whitelistDirectory), workspace.WhitelistDirectory);
        Assert.Equal(Path.GetFullPath(includeDirectory), workspace.IncludeDirectory);
        Assert.Equal(Path.GetFullPath(hierarchyDirectory), workspace.HierarchyDirectory);
    }
}
