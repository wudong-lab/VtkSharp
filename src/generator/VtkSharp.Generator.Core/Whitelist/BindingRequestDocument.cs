namespace VtkSharp.Generator.Core.Whitelist;

public sealed record BindingRequestDocument
{
    public CandidateSource? Source { get; init; }
    public List<BindingRequest> Requests { get; init; } = [];
}

public sealed record BindingRequest
{
    public string Class { get; init; } = "";
    public List<string> Methods { get; init; } = [];
    public List<string> Signatures { get; init; } = [];
    public bool ClassOnly { get; init; }
    public bool AllOverloads { get; init; }
}

public sealed record BindingRequestDiagnostic(
    string Class, string Request, string Status, string? DeclaringClass = null,
    string? Reason = null, IReadOnlyList<string>? Signatures = null);

public sealed record BindingRequestPlan(CandidateDocument Candidate, IReadOnlyList<BindingRequestDiagnostic> Diagnostics)
{
    public bool HasUnresolved => Diagnostics.Any(item => item.Status is not ("ready" or "already-exported"));
}
