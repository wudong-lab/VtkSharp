namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistLength
{
    public string Kind { get; init; } = "";
    public int? Value { get; init; }
    public string? Name { get; init; }
}