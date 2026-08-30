using VtkSharp.Generator.Core.Inspection;

namespace VtkSharp.Generator.Core.Whitelist;

internal static class EnumCandidateExpansion
{
    internal static void Expand(CandidateRequirement requirement, InspectedClass inspected, IEnumerable<string>? requestedNames = null)
    {
        var names = (requestedNames ?? requirement.Functions.Select(f => f.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var property in inspected.EnumProperties ?? [])
        {
            if (!property.Methods.Any(names.Contains)) continue;
            requirement.EnumProperties ??= [];
            if (!requirement.EnumProperties.Any(p => p.Name == property.Name)) requirement.EnumProperties.Add(property);
            foreach (var function in inspected.Functions.Where(f => property.Methods.Contains(f.Name)))
            {
                var id = CandidateMergePlan.FunctionId(inspected.Name, function.ReturnType, function.Name, function.Parameters.Select(p => p.Type));
                if (!requirement.Functions.Any(f => CandidateMergePlan.FunctionId(inspected.Name, f) == id))
                    requirement.Functions.Add(CandidateWhitelistService.ToWhitelistFunction(function));
            }
        }
    }
}
