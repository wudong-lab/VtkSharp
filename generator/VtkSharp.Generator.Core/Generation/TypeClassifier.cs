namespace VtkSharp.Generator.Core.Generation;

public static class TypeClassifier
{
    private static readonly HashSet<string> VtkValueStructs = new(StringComparer.Ordinal)
    {
        "vtkColor3d",
    };

    private static readonly Dictionary<string, (int Count, string CSharpName, string CppHeader)> ValueStructInfo = new()
    {
        ["vtkColor3d"] = (3, "VtkColor3d", "vtkColor"),
    };

    public static bool IsVtkValueStruct(string type) => VtkValueStructs.Contains(type);

    public static int GetValueStructComponentCount(string type)
        => ValueStructInfo.TryGetValue(type, out var info) ? info.Count : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string GetValueStructCSharpName(string type)
        => ValueStructInfo.TryGetValue(type, out var info) ? info.CSharpName : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string? GetValueStructCppHeader(string type)
        => ValueStructInfo.TryGetValue(type, out var info) ? info.CppHeader : null;
}
