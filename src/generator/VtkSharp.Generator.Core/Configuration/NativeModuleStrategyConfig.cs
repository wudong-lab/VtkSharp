namespace VtkSharp.Generator.Core.Configuration;

public sealed record NativeModuleStrategyConfig
{
    public int SchemaVersion { get; init; }
    public int StrategyVersion { get; init; }
    public string VtkVersion { get; init; } = "";
    public string Platform { get; init; } = "";
    public List<NativeModuleGroupConfig> Groups { get; init; } = [];
    public List<ManualNativeSourceOwnershipConfig> ManualSourceOwnership { get; init; } = [];
}

public sealed record NativeModuleGroupConfig
{
    public string Name { get; init; } = "";
    public string Target { get; init; } = "";
    public List<string> Modules { get; init; } = [];
    public List<string> InitializationProviders { get; init; } = [];
    public string Rationale { get; init; } = "";
}

public sealed record ManualNativeSourceOwnershipConfig
{
    public string Source { get; init; } = "";
    public string Target { get; init; } = "";
    public string Rationale { get; init; } = "";
}
