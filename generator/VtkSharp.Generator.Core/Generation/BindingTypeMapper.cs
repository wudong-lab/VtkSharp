namespace VtkSharp.Generator.Core.Generation;

public static class BindingTypeMapper
{
    private static readonly Dictionary<string, string> CSharpPublicScalarTypes = new(StringComparer.Ordinal)
    {
        ["void"] = "void",
        ["char"] = "char",
        ["int"] = "int",
        ["unsigned int"] = "uint",
        ["unsigned long"] = "ulong",
        ["long long"] = "long",
        ["unsigned long long"] = "ulong",
        ["double"] = "double",
        ["float"] = "float",
        ["bool"] = "bool",
        ["vtkTypeBool"] = "bool",
        ["vtkTypeUInt32"] = "uint",
        ["vtkIdType"] = "long",
        ["const char*"] = "string",
        ["char*"] = "string",
        ["void*"] = "nint",
        ["HWND"] = "nint",
        ["HDC"] = "nint",
        ["HGLRC"] = "nint",
    };

    private static readonly Dictionary<string, string> CSharpInteropScalarTypes = new(StringComparer.Ordinal)
    {
        ["void"] = "void",
        ["char"] = "char",
        ["int"] = "int",
        ["unsigned int"] = "uint",
        ["unsigned long"] = "ulong",
        ["long long"] = "long",
        ["unsigned long long"] = "ulong",
        ["double"] = "double",
        ["float"] = "float",
        ["bool"] = "bool",
        ["vtkTypeBool"] = "int",
        ["vtkTypeUInt32"] = "uint",
        ["vtkIdType"] = "long",
        ["const char*"] = "nint",
        ["char*"] = "nint",
        ["void*"] = "nint",
        ["HWND"] = "nint",
        ["HDC"] = "nint",
        ["HGLRC"] = "nint",
    };

    private static readonly Dictionary<string, string> CppTypes = new(StringComparer.Ordinal)
    {
        ["void"] = "void",
        ["char"] = "char",
        ["int"] = "int",
        ["unsigned int"] = "unsigned int",
        ["unsigned long"] = "unsigned long",
        ["long long"] = "long long",
        ["unsigned long long"] = "unsigned long long",
        ["double"] = "double",
        ["float"] = "float",
        ["bool"] = "bool",
        ["vtkTypeBool"] = "vtkTypeBool",
        ["vtkTypeUInt32"] = "vtkTypeUInt32",
        ["vtkIdType"] = "vtkIdType",
        ["const char*"] = "const char*",
        ["char*"] = "char*",
        ["void*"] = "void*",
        ["HWND"] = "void*",
        ["HDC"] = "void*",
        ["HGLRC"] = "void*",
    };

    private static readonly HashSet<string> FixedArrayElementTypes = new(StringComparer.Ordinal)
    {
        "double", "float", "int",
    };

    public static bool IsSupportedType(string type)
        => CSharpPublicScalarTypes.ContainsKey(type) ||
           TypeClassifier.IsVtkValueStruct(type) ||
           TypeClassifier.TryGetVtkClassPointerName(type, out _) ||
           IsFixedArrayWithSupportedElement(type) ||
           TypeClassifier.IsSupportedPrimitivePointerType(type);

    public static bool IsStringPointer(string type)
        => type is "const char*" or "char*";

    public static bool IsFixedArray(string type)
        => type.EndsWith(']') && type.Contains('[', StringComparison.Ordinal);

    public static bool IsFixedArrayWithSupportedElement(string type)
        => IsFixedArray(type) && FixedArrayElementTypes.Contains(GetArrayElementType(type));

    public static string GetArrayElementType(string type)
    {
        var bracketIndex = type.IndexOf('[', StringComparison.Ordinal);
        return type[..bracketIndex].Replace("const ", "", StringComparison.Ordinal).Trim();
    }

    public static string ToCSharpPublicType(string type)
    {
        if (TypeClassifier.IsVtkValueStruct(type))
            return TypeClassifier.GetValueStructCSharpName(type);

        if (TypeClassifier.TryGetVtkClassPointerName(type, out var className))
            return className;

        if (CSharpPublicScalarTypes.TryGetValue(type, out var scalarType))
            return scalarType;

        if (TypeClassifier.IsSupportedPrimitivePointerType(type))
            return $"{ToCSharpPublicType(TypeClassifier.GetPointerElementType(type))}*";

        if (IsFixedArray(type))
            return type.StartsWith("const ", StringComparison.Ordinal)
                ? $"ReadOnlySpan<{GetArrayElementType(type)}>"
                : $"Span<{GetArrayElementType(type)}>";

        throw new NotSupportedException($"C# public type '{type}' is not supported by the MVP emitter.");
    }

    public static string ToCSharpInteropType(string type)
    {
        if (TypeClassifier.IsVtkValueStruct(type))
            return "void";

        if (TypeClassifier.TryGetVtkClassPointerName(type, out _))
            return "nint";

        if (CSharpInteropScalarTypes.TryGetValue(type, out var scalarType))
            return scalarType;

        if (TypeClassifier.IsSupportedPrimitivePointerType(type))
            return $"{ToCSharpInteropType(TypeClassifier.GetPointerElementType(type))}*";

        if (IsFixedArray(type))
            return $"{GetArrayElementType(type)}*";

        throw new NotSupportedException($"C# interop type '{type}' is not supported by the MVP emitter.");
    }

    public static string ToCSharpInteropReturnType(string type)
        => type == "char" ? "byte" : ToCSharpInteropType(type);

    public static string ToCSharpInteropParameterType(string type)
        => IsStringPointer(type) ? "string" :
           type == "char" ? "byte" :
           ToCSharpInteropType(type);

    public static string ToCppType(string type)
    {
        if (CppTypes.TryGetValue(type, out var cppType))
            return cppType;

        if (TypeClassifier.IsVtkValueStruct(type))
            return "void";

        if (TypeClassifier.IsSupportedPrimitivePointerType(type))
            return type;

        if (IsFixedArray(type))
            return ToCppArrayPointerType(type);

        if (IsVtkPointer(type))
            return type;

        throw new NotSupportedException($"C++ type '{type}' is not supported by the MVP emitter.");
    }

    public static string ToCppExportType(string type)
        => type == "unsigned long" ? "std::uint64_t" : ToCppType(type);

    public static string ToCppArrayPointerType(string type)
    {
        var isConst = type.StartsWith("const ", StringComparison.Ordinal);
        var elementType = GetArrayElementType(type);
        return isConst ? $"const {elementType}*" : $"{elementType}*";
    }

    private static bool IsVtkPointer(string type)
    {
        var normalized = type.Replace("const", "", StringComparison.Ordinal).Trim();
        return normalized.StartsWith("vtk", StringComparison.Ordinal) && normalized.EndsWith("*", StringComparison.Ordinal);
    }
}
