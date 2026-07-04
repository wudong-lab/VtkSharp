namespace VtkSharp.Generator.Core.Exporting;

public sealed record TypeFunctionInventory(
    string SelectedTypeName,
    IReadOnlyList<FunctionExportGroup> AlreadyExported,
    IReadOnlyList<FunctionExportGroup> AvailableToAdd,
    IReadOnlyList<FunctionExportGroup> Unsupported);
