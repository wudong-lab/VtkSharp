using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Configuration;

public sealed class GeneratorConfigLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public GeneratorConfig Load(string configPath, string? localConfigPath = null, string? vtkRootOverride = null)
    {
        var config = this.ReadRequired<GeneratorConfig>(configPath);
        var local = localConfigPath is not null && File.Exists(localConfigPath)
            ? this.ReadRequired<LocalGeneratorConfig>(localConfigPath)
            : null; 

        var vtkRoot = vtkRootOverride
            ?? Environment.GetEnvironmentVariable("VTK_ROOT")
            ?? local?.Vtk.RootDirectory
            ?? config.Vtk.RootDirectory;

        var vtk = config.Vtk with
        {
            RootDirectory = vtkRoot,
            IncludeDirectory = local?.Vtk.IncludeDirectory ?? config.Vtk.IncludeDirectory,
            HierarchyDirectory = local?.Vtk.HierarchyDirectory ?? config.Vtk.HierarchyDirectory,
        };

        return config with { Vtk = vtk };
    }

    private T ReadRequired<T>(string path)
    {
        using var reader = File.OpenText(path);
        return this._deserializer.Deserialize<T>(reader);
    }

    private sealed record LocalGeneratorConfig
    {
        public LocalVtkConfig Vtk { get; init; } = new();
    }

    private sealed record LocalVtkConfig
    {
        public string? RootDirectory { get; init; }
        public string? IncludeDirectory { get; init; }
        public string? HierarchyDirectory { get; init; }
    }
}
