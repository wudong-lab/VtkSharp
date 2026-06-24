namespace VtkSharp.Generator.Core.Inspection;

public sealed record InspectedFunction(
    string Name,
    string CppSignature,
    string ReturnType,
    IReadOnlyList<InspectedParameter> Parameters,
    bool IsSupported,
    string? CanonicalSignature = null,
    IReadOnlyList<string>? DependencyTypes = null);