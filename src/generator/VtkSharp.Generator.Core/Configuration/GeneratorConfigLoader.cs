using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using System.Text.Json;
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

    public NativeModuleStrategyConfig LoadNativeModuleStrategy(string path)
        => this.ReadRequired<NativeModuleStrategyConfig>(path);

    public NativeModuleMetadataSnapshot LoadNativeModuleMetadata(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<NativeModuleMetadataSnapshot>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidDataException($"Native module metadata snapshot is empty: '{path}'.");
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
