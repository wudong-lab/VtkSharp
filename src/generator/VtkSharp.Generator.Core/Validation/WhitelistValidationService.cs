using System.Text.Json;
using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Core.Validation;

public sealed class WhitelistValidationService
{
    public int Validate(GeneratorRunContext context, bool continueOnError, string format, TextWriter output, TextWriter error)
    {
        var diagnostics = new List<ValidationDiagnostic>(context.InspectionDiagnostics);
        var validator = new WhitelistValidator();

        foreach (var document in context.Documents)
        {
            diagnostics.AddRange(validator.Validate(document, context.InspectedClasses, context.HierarchyResolver).Diagnostics);
        }

        if (diagnostics.Count == 0)
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine(JsonSerializer.Serialize(new { ok = true, errors = Array.Empty<string>() }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                output.WriteLine("Whitelist validation succeeded.");
            }

            return 0;
        }

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, errors = diagnostics.Select(d => d.Message) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                error.WriteLine(diagnostic.Message);
            }
        }

        return continueOnError ? 0 : 1;
    }
}