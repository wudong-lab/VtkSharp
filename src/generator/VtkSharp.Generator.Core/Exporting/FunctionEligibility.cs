using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;

namespace VtkSharp.Generator.Core.Exporting;

public sealed record FunctionEligibility(string Status, string? Reason = null)
{
    public static FunctionEligibility Evaluate(InspectedFunction function)
    {
        foreach (var type in function.Parameters.Select(parameter => parameter.Type).Append(function.ReturnType))
            if (!WhitelistValidator.IsSupportedType(type))
                return new("unsupported", $"Unsupported type '{type}'.");

        foreach (var parameter in function.Parameters)
            if (TypeClassifier.IsSupportedPrimitivePointerType(parameter.Type))
                return new("needs-metadata", $"Parameter '{parameter.Name}' ({parameter.Type}) requires direction and length metadata.");

        return new("ready");
    }
}
