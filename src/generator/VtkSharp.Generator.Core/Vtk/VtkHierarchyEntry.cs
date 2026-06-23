namespace VtkSharp.Generator.Core.Vtk;

public sealed record VtkHierarchyEntry(
    string ClassName,
    string BaseClassName,
    string Header,
    string Module);
