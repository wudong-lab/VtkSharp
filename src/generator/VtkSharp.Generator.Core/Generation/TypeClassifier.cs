namespace VtkSharp.Generator.Core.Generation;

public static class TypeClassifier
{
    private sealed record ValueStructInfo(
        int ComponentCount,
        string CSharpName,
        string CppHeader,
        string CppElementType,
        string CSharpElementType,
        IReadOnlyList<string> ComponentNames);

    private static readonly HashSet<string> VtkScalarTypes = new(StringComparer.Ordinal)
    {
        "vtkTypeBool",
        "vtkTypeUInt32",
        "vtkIdType",
        "vtkMTimeType",
    };

    private static readonly HashSet<string> VtkValueStructs = new(StringComparer.Ordinal)
    {
        "vtkColor3d",
        "vtkColor3ub",
    };

    private static readonly HashSet<string> SupportedPrimitivePointerElementTypes = new(StringComparer.Ordinal)
    {
        "double",
        "float",
        "int",
        "vtkIdType",
    };

    private static readonly Dictionary<string, ValueStructInfo> ValueStructs = new()
    {
        ["vtkColor3d"] = new(3, "VtkColor3d", "vtkColor", "double", "double", ["R", "G", "B"]),
        ["vtkColor3ub"] = new(3, "VtkColor3ub", "vtkColor", "unsigned char", "byte", ["R", "G", "B"]),
    };

    public static bool IsVtkValueStruct(string type) => VtkValueStructs.Contains(type);

    public static bool IsVtkScalarType(string type) => VtkScalarTypes.Contains(type);

    public static bool IsSupportedPrimitivePointerType(string type)
        => type.TrimEnd().EndsWith('*') && SupportedPrimitivePointerElementTypes.Contains(GetPointerElementType(type));

    public static string GetPointerElementType(string type)
        => type.Replace("const", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal)
            .Trim();

    public static string? ExtractVtkClassName(string type)
    {
        var normalized = type.Replace("const", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal)
            .Replace("&", "", StringComparison.Ordinal)
            .Trim();

        var nestedTypeSeparator = normalized.IndexOf("::", StringComparison.Ordinal);
        if (nestedTypeSeparator >= 0)
            normalized = normalized[..nestedTypeSeparator];

        return normalized.StartsWith("vtk", StringComparison.Ordinal) &&
               !IsVtkScalarType(normalized) &&
               !IsVtkValueStruct(normalized)
            ? normalized
            : null;
    }

    public static bool TryGetVtkClassPointerName(string type, out string className)
    {
        if (type.Replace("const", "", StringComparison.Ordinal).Trim().EndsWith("*", StringComparison.Ordinal))
        {
            var extractedClassName = ExtractVtkClassName(type);
            if (extractedClassName is not null)
            {
                className = extractedClassName;
                return true;
            }
        }

        className = "";
        return false;
    }

    public static int GetValueStructComponentCount(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.ComponentCount : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string GetValueStructCSharpName(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.CSharpName : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string? GetValueStructCppHeader(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.CppHeader : null;

    public static string GetValueStructCppElementType(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.CppElementType : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string GetValueStructCSharpElementType(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.CSharpElementType : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static IReadOnlyList<string> GetValueStructComponentNames(string type)
        => ValueStructs.TryGetValue(type, out var info) ? info.ComponentNames : throw new NotSupportedException($"Unknown value struct type '{type}'.");
}
