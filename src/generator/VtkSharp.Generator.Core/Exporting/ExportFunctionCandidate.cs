namespace VtkSharp.Generator.Core.Exporting;

public sealed record ExportFunctionCandidate(
    string Id,
    string SelectedTypeName,
    string DeclaringTypeName,
    string Module,
    string Header,
    string Signature,
    string FunctionName,
    string ReturnType,
    IReadOnlyList<ExportParameterCandidate> Parameters,
    ExportStatus Status,
    bool CanSelectForExport,
    string? Reason,
    VtkSharp.Generator.Core.Whitelist.EnumProperty? EnumProperty = null,
    IReadOnlyList<VtkSharp.Generator.Core.Inspection.InspectedFunction>? EnumFunctions = null);
