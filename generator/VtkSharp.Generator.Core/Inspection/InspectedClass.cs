namespace VtkSharp.Generator.Core.Inspection;

public sealed record InspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions,
    bool HasStaticNew = false,
    string? BaseClassName = null,
    IReadOnlyList<string>? Dependencies = null,
    IReadOnlyList<string>? BaseClassNames = null,
    string? Module = null,
    string? Header = null);
