namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistParameter
{
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Direction { get; init; }
    public WhitelistLength? Length { get; init; }
}