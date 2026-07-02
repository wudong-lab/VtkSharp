namespace VtkSharp.Generator.Core.Inspection;

internal sealed record RawInspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions,
    bool HasStaticNew,
    IReadOnlyList<string> BaseClassNames)
{
    public InspectedClass ToInspectedClassWithBaseClassNames()
        => new(this.Name, this.Functions, this.HasStaticNew, BaseClassNames: this.BaseClassNames);
}