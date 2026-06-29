using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Validation;

public sealed class WhitelistValidator
{
    // Canonical scalar types supported by both C# and C++ emitters.
    private static readonly HashSet<string> SupportedScalarTypes = new(StringComparer.Ordinal)
    {
        "void", "char", "int", "unsigned int", "long long", "unsigned long long",
        "double", "float", "bool", "vtkTypeBool", "vtkTypeUInt32", "vtkIdType",
        "const char*", "char*", "void*",
        "HWND", "HDC", "HGLRC",
    };

    // Element types that are valid inside T[N] / const T[N].
    private static readonly HashSet<string> FixedArrayElementTypes = new(StringComparer.Ordinal)
    {
        "double", "float", "int",
    };

    // Primitive pointer types allowed for return values and for parameters
    // that carry direction + length metadata.
    private static readonly HashSet<string> PrimitivePointerTypes = new(StringComparer.Ordinal)
    {
        "double*", "float*", "int*",
    };

    public ValidationResult Validate(WhitelistDocument document, IReadOnlyDictionary<string, InspectedClass> inspectedClasses, VtkHierarchyResolver? hierarchyResolver = null)
    {
        var diagnostics = new List<ValidationDiagnostic>();

        foreach (var whitelistClass in document.Classes)
        {
            if (!inspectedClasses.TryGetValue(whitelistClass.Name, out var inspectedClass))
            {
                diagnostics.Add(new ValidationDiagnostic($"Class '{whitelistClass.Name}' was not inspected."));
                continue;
            }

            if (hierarchyResolver is not null)
            {
                var hierarchyModule = hierarchyResolver.GetModule(whitelistClass.Name);
                if (!string.IsNullOrWhiteSpace(hierarchyModule) && !hierarchyModule.Equals(document.Module, StringComparison.Ordinal))
                    diagnostics.Add(new ValidationDiagnostic(
                        $"Class '{whitelistClass.Name}' belongs to module '{hierarchyModule}' in hierarchy but is declared in '{document.Module}' whitelist."));

                var hierarchyHeader = hierarchyResolver.GetHeader(whitelistClass.Name);
                if (!hierarchyHeader.Equals(whitelistClass.Header, StringComparison.Ordinal))
                    diagnostics.Add(new ValidationDiagnostic(
                        $"Class '{whitelistClass.Name}' has header '{hierarchyHeader}' in hierarchy but whitelist declares '{whitelistClass.Header}'."));
            }

            foreach (var function in whitelistClass.Functions ?? [])
            {
                var matches = inspectedClass.Functions
                    .Where(item => item.Name == function.Name)
                    .Where(item => item.ReturnType == function.Return.Type)
                    .Where(item => item.Parameters.Select(p => p.Type).SequenceEqual(function.Parameters.Select(p => p.Type)))
                    .ToList();

                if (matches.Count == 0)
                    diagnostics.Add(new ValidationDiagnostic($"Function '{whitelistClass.Name}.{function.Name}' was not found."));
                else if (matches.Count > 1)
                    diagnostics.Add(new ValidationDiagnostic($"Function '{whitelistClass.Name}.{function.Name}' matched multiple overloads."));

                diagnostics.AddRange(ValidateTypes(whitelistClass.Name, function));
            }
        }

        return new ValidationResult(diagnostics);
    }

    private static IEnumerable<ValidationDiagnostic> ValidateTypes(string className, WhitelistFunction function)
    {
        var diagnostics = new List<ValidationDiagnostic>();

        diagnostics.AddRange(CheckType(className, function.Name, "return", function.Return.Type));
        foreach (var parameter in function.Parameters)
        {
            diagnostics.AddRange(CheckParameterType(className, function.Name, parameter));
        }

        return diagnostics;
    }

    private static List<ValidationDiagnostic> CheckParameterType(string className, string functionName, WhitelistParameter parameter)
    {
        var diagnostics = new List<ValidationDiagnostic>(CheckType(className, functionName, $"parameter '{parameter.Name}'", parameter.Type));

        if (PrimitivePointerTypes.Contains(parameter.Type) && parameter.Direction is null && parameter.Length is null)
        {
            diagnostics.Add(new ValidationDiagnostic(
                $"Type '{className}.{functionName}' parameter '{parameter.Name}' ({parameter.Type}) is a primitive pointer " +
                "but has no direction or length metadata. Add direction: in/out and length.kind: fixed/parameter to the whitelist entry."));
        }

        return diagnostics;
    }

    private static IEnumerable<ValidationDiagnostic> CheckType(string className, string functionName, string role, string type)
    {
        if (IsSupportedType(type))
            return [];

        // Determine a more specific hint.
        var hint = type switch
        {
            _ when type.EndsWith(']') && type.Contains('[', StringComparison.Ordinal) =>
                $"unsupported fixed-array element type '{GetArrayElementType(type)}' in '{type}'",
            _ when PrimitivePointerTypes.Contains(type) =>
                "primitive pointer — add direction and length metadata to the parameter entry",
            _ when TypeClassifier.IsVtkValueStruct(type) =>
                $"vtk value struct '{type}' requires emitter support (out-pointer bridge)",
            _ when type.StartsWith("vtk", StringComparison.Ordinal) && !type.EndsWith('*') =>
                $"vtk class name '{type}' without pointer — did you mean '{type}*'?",
            _ => $"unsupported type '{type}'",
        };

        return [new ValidationDiagnostic($"Type '{className}.{functionName}' {role}: {hint}.")];
    }

    public static bool IsSupportedType(string type)
    {
        if (SupportedScalarTypes.Contains(type))
            return true;

        if (TypeClassifier.IsVtkValueStruct(type))
            return true;

        if (IsVtkClassPointer(type))
            return true;

        if (IsFixedArrayWithSupportedElement(type))
            return true;

        if (PrimitivePointerTypes.Contains(type))
            return true;

        return false;
    }

    private static bool IsVtkClassPointer(string type)
    {
        var normalized = type.Replace("const ", "", StringComparison.Ordinal);
        return normalized.StartsWith("vtk", StringComparison.Ordinal) &&
               normalized.EndsWith('*');
    }

    private static bool IsFixedArrayWithSupportedElement(string type)
        => type.EndsWith(']') && type.Contains('[', StringComparison.Ordinal) &&
           FixedArrayElementTypes.Contains(GetArrayElementType(type));

    private static string GetArrayElementType(string type)
    {
        var bracketIndex = type.IndexOf('[', StringComparison.Ordinal);
        return type[..bracketIndex].Replace("const ", "", StringComparison.Ordinal).Trim();
    }
}
