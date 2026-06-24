namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistDocument
{
    public string Module { get; init; } = "";
    public List<WhitelistClass> Classes { get; init; } = [];
}