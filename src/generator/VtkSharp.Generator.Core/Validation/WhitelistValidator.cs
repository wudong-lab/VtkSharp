using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Validation;

public sealed class WhitelistValidator
{
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
        if (function.Return.Ownership is not null &&
            (function.Return.Ownership is not ("owned" or "borrowed") ||
             !TypeClassifier.TryGetVtkClassPointerName(function.Return.Type, out _)))
            diagnostics.Add(new ValidationDiagnostic($"Function '{className}.{function.Name}' ownership must be owned/borrowed and apply to a VTK object pointer return."));
        foreach (var parameter in function.Parameters)
        {
            diagnostics.AddRange(CheckParameterType(className, function.Name, parameter));
            if (parameter.Length is { } length)
            {
                var valid = length.Kind switch
                {
                    "fixed" => length.Value is > 0 && length.Name is null,
                    "parameter" => length.Value is null && function.Parameters.Count(p =>
                        p.Name == length.Name && p.Name != parameter.Name &&
                        p.Type is "char" or "unsigned char" or "int" or "unsigned int" or "short" or "unsigned short" or
                            "long" or "unsigned long" or "long long" or "unsigned long long" or "vtkIdType" or "vtkTypeUInt32") == 1,
                    _ => false,
                };
                if (!valid)
                    diagnostics.Add(new ValidationDiagnostic($"Function '{className}.{function.Name}' parameter '{parameter.Name}' has invalid length metadata. Use a positive fixed value or reference an integer count parameter."));
            }
        }

        return diagnostics;
    }

    private static List<ValidationDiagnostic> CheckParameterType(string className, string functionName, WhitelistParameter parameter)
    {
        var diagnostics = new List<ValidationDiagnostic>(CheckType(className, functionName, $"parameter '{parameter.Name}'", parameter.Type));

        if (TypeClassifier.IsSupportedPrimitivePointerType(parameter.Type) && (parameter.Direction is null || parameter.Length is null))
        {
            diagnostics.Add(new ValidationDiagnostic(
                $"Type '{className}.{functionName}' parameter '{parameter.Name}' ({parameter.Type}) is a primitive pointer " +
                "but lacks complete direction and length metadata. Add direction: in/out/inout and length.kind: fixed/parameter to the whitelist entry."));
        }

        if (parameter.Direction is not null && parameter.Direction is not ("in" or "out" or "inout"))
            diagnostics.Add(new ValidationDiagnostic($"Function '{className}.{functionName}' parameter '{parameter.Name}' has invalid direction metadata."));
        if ((parameter.Direction is not null || parameter.Length is not null) &&
            !TypeClassifier.IsSupportedPrimitivePointerType(parameter.Type) && !BindingTypeMapper.IsFixedArray(parameter.Type))
            diagnostics.Add(new ValidationDiagnostic($"Function '{className}.{functionName}' parameter '{parameter.Name}' direction/length metadata requires an array or primitive pointer."));

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
                $"unsupported fixed-array element type '{BindingTypeMapper.GetArrayElementType(type)}' in '{type}'",
            _ when TypeClassifier.IsSupportedPrimitivePointerType(type) =>
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
        => BindingTypeMapper.IsSupportedType(type);
}
