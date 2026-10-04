namespace VtkSharp.Generator.Core.Configuration;

public sealed record NativeModuleMetadataSnapshot
{
    public int SchemaVersion { get; init; }
    public string VtkVersion { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Configuration { get; init; } = "";
    public List<NativeModuleMetadata> Modules { get; init; } = [];
}

public sealed record NativeModuleMetadata
{
    public string Module { get; init; } = "";
    public List<string> Depends { get; init; } = [];
    public List<string> PrivateDepends { get; init; } = [];
    public List<string> OptionalDepends { get; init; } = [];
    public List<string> Implements { get; init; } = [];
    public bool Implementable { get; init; }
    public string LibraryName { get; init; } = "";
    public bool NeedsAutoinit { get; init; }
}
