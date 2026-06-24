namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistClass
{
    public string Name { get; init; } = "";
    public string Header { get; init; } = "";
    public List<WhitelistFunction> Functions { get; init; } = [];
}