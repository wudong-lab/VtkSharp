namespace VtkSharp.Generator.Core.Generation;

public static class TypeClassifier
{
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
    };

    private static readonly HashSet<string> SupportedPrimitivePointerElementTypes = new(StringComparer.Ordinal)
    {
        "double",
        "float",
        "int",
        "vtkIdType",
    };

    private static readonly Dictionary<string, (int Count, string CSharpName, string CppHeader)> ValueStructInfo = new()
    {
        ["vtkColor3d"] = (3, "VtkColor3d", "vtkColor"),
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
        => ValueStructInfo.TryGetValue(type, out var info) ? info.Count : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string GetValueStructCSharpName(string type)
        => ValueStructInfo.TryGetValue(type, out var info) ? info.CSharpName : throw new NotSupportedException($"Unknown value struct type '{type}'.");

    public static string? GetValueStructCppHeader(string type)
        => ValueStructInfo.TryGetValue(type, out var info) ? info.CppHeader : null;
}
