namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistReturn
{
    public string Type { get; init; } = "";
    public string? Ownership { get; init; }
}