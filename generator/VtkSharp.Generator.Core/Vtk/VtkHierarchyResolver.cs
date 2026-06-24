namespace VtkSharp.Generator.Core.Vtk;

public sealed class VtkHierarchyResolver
{
    private readonly IReadOnlyDictionary<string, VtkHierarchyEntry> _entries;

    public VtkHierarchyResolver(IReadOnlyDictionary<string, VtkHierarchyEntry> entries)
    {
        this._entries = entries;
    }

    public string GetBaseClassName(string className)
        => this._entries.TryGetValue(className, out var entry) && !string.IsNullOrWhiteSpace(entry.BaseClassName)
            ? entry.BaseClassName
            : "vtkObject";

    public string GetHeader(string className)
        => this._entries.TryGetValue(className, out var entry) && !string.IsNullOrWhiteSpace(entry.Header)
            ? entry.Header
            : $"{className}.h";
}