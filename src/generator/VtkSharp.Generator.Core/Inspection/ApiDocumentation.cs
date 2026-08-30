namespace VtkSharp.Generator.Core.Inspection;

public sealed record ApiDocumentation(
    string? Summary,
    string? Remarks = null,
    IReadOnlyList<ParameterDocumentation>? Parameters = null,
    string? Returns = null);

public sealed record ParameterDocumentation(string Name, string Text, string? Direction = null);
