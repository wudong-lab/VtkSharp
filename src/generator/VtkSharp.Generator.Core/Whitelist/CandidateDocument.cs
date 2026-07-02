namespace VtkSharp.Generator.Core.Whitelist;

// Matches schema: generator/schemas/vtksharp.whitelist-candidate.schema.json

public sealed record CandidateDocument
{
    public string Status { get; init; } = "";
    public CandidateSource? Source { get; init; }
    public List<CandidateRequirement> Requirements { get; init; } = [];
}

public sealed record CandidateSource
{
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string Original { get; init; } = "";
}

public sealed record CandidateRequirement
{
    public string Module { get; init; } = "";
    public string Class { get; init; } = "";
    public string Header { get; init; } = "";
    public List<WhitelistFunction> Functions { get; init; } = [];
}
