namespace VtkSharp.Generator.Core.Configuration;

public sealed record PathConfig
{
    public string WhitelistDirectory { get; init; } = "";
    public string ManagedOutputDirectory { get; init; } = "";
    public string NativeOutputDirectory { get; init; } = "";
    public string NativeProjectFile { get; init; } = "";
    public string NativeModulesFile { get; init; } = "";
}
