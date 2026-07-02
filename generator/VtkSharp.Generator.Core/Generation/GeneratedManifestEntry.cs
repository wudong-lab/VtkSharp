namespace VtkSharp.Generator.Core.Generation;

public sealed record GeneratedManifestEntry
{
    public string ClassName { get; init; } = "";
    public string Header { get; init; } = "";
    public string BaseClassName { get; init; } = "";
    public bool HasStaticNew { get; init; }
    public string InputHash { get; init; } = "";
    public string ManagedPath { get; init; } = "";
    public string NativePath { get; init; } = "";
    public string ManagedContentHash { get; init; } = "";
    public string NativeContentHash { get; init; } = "";
}