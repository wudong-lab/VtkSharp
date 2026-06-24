using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Validation;

public sealed record ValidationDiagnostic(string Message);

public sealed record ValidationResult(IReadOnlyList<ValidationDiagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}

public sealed class WhitelistValidator
{
    public ValidationResult Validate(WhitelistDocument document, IReadOnlyDictionary<string, InspectedClass> inspectedClasses)
    {
        var diagnostics = new List<ValidationDiagnostic>();

        foreach (var whitelistClass in document.Classes)
        {
            if (!inspectedClasses.TryGetValue(whitelistClass.Name, out var inspectedClass))
            {
                diagnostics.Add(new ValidationDiagnostic($"Class '{whitelistClass.Name}' was not inspected."));
                continue;
            }

            foreach (var function in whitelistClass.Functions)
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
            }
        }

        return new ValidationResult(diagnostics);
    }
}
