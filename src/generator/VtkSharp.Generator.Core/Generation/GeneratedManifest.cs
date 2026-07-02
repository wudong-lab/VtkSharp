namespace VtkSharp.Generator.Core.Generation;

public sealed record GeneratedManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string GeneratorVersion { get; init; } = "";
    public string Module { get; init; } = "";
    public List<GeneratedManifestEntry> Classes { get; init; } = [];
}