namespace VtkSharp.Generator.Core.Exporting;

public sealed record ExportPlan(
    IReadOnlyList<ExportFunctionCandidate> Functions,
    IReadOnlyList<string> Diagnostics);
