namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistFunction
{
    public string Name { get; init; } = "";
    public string CppSignature { get; init; } = "";
    public WhitelistReturn Return { get; init; } = new();
    public List<WhitelistParameter> Parameters { get; init; } = [];
}