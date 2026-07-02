using System.Text.Json;

namespace VtkSharp.Generator.Core.Generation;

public sealed class GeneratedManifestStore
{
    public const int CurrentSchemaVersion = 1;
    public const string FileName = ".vtksharp.generated.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public GeneratedManifest Load(string path, string module, string generatorVersion)
    {
        if (!File.Exists(path))
            return CreateEmpty(module, generatorVersion);

        try
        {
            var manifest = JsonSerializer.Deserialize<GeneratedManifest>(File.ReadAllText(path), JsonOptions);
            if (manifest is null ||
                manifest.SchemaVersion != CurrentSchemaVersion ||
                !manifest.GeneratorVersion.Equals(generatorVersion, StringComparison.Ordinal) ||
                !manifest.Module.Equals(module, StringComparison.Ordinal))
            {
                return CreateEmpty(module, generatorVersion);
            }

            return manifest;
        }
        catch (JsonException)
        {
            return CreateEmpty(module, generatorVersion);
        }
    }

    public void Save(string path, GeneratedManifest manifest)
    {
        var directory = Path.GetDirectoryName(path)
                        ?? throw new InvalidOperationException($"Manifest path '{path}' does not have a directory.");
        Directory.CreateDirectory(directory);
        GeneratedFileWriter.WriteIfChanged(path, JsonSerializer.Serialize(manifest, JsonOptions) + Environment.NewLine);
    }

    public static GeneratedManifest WithEntry(GeneratedManifest manifest, GeneratedManifestEntry entry)
    {
        var classes = manifest.Classes
            .Where(item => !item.ClassName.Equals(entry.ClassName, StringComparison.Ordinal))
            .Append(entry)
            .OrderBy(item => item.ClassName, StringComparer.Ordinal)
            .ToList();

        return manifest with { Classes = classes };
    }

    private static GeneratedManifest CreateEmpty(string module, string generatorVersion)
        => new()
        {
            SchemaVersion = CurrentSchemaVersion,
            GeneratorVersion = generatorVersion,
            Module = module,
            Classes = [],
        };
}
