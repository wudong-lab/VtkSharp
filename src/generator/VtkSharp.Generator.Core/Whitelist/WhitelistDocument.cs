namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistDocument
{
    public string Module { get; init; } = "";
    public List<WhitelistClass> Classes { get; init; } = [];
}

public sealed record WhitelistClass
{
    public string Name { get; init; } = "";
    public string Header { get; init; } = "";
    public List<WhitelistFunction> Functions { get; init; } = [];
}

public sealed record WhitelistFunction
{
    public string Name { get; init; } = "";
    public string CppSignature { get; init; } = "";
    public WhitelistReturn Return { get; init; } = new();
    public List<WhitelistParameter> Parameters { get; init; } = [];
}

public sealed record WhitelistReturn
{
    public string Type { get; init; } = "";
    public string? Ownership { get; init; }
}

public sealed record WhitelistParameter
{
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Direction { get; init; }
    public WhitelistLength? Length { get; init; }
}

public sealed record WhitelistLength
{
    public string Kind { get; init; } = "";
    public int? Value { get; init; }
    public string? Name { get; init; }
}
