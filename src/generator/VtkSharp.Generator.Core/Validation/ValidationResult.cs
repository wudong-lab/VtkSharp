namespace VtkSharp.Generator.Core.Validation;

public sealed record ValidationResult(IReadOnlyList<ValidationDiagnostic> Diagnostics)
{
    public bool Success => this.Diagnostics.Count == 0;
}