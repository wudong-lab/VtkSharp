namespace VtkSharp.Generator.Core.Exporting;

public sealed record FunctionExportGroup(
    string DeclaringTypeName,
    IReadOnlyList<ExportFunctionCandidate> Functions);
