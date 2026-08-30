namespace VtkSharp.Generator.Core.Inspection;

internal sealed record RawInspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions,
    bool HasStaticNew,
    IReadOnlyList<string> BaseClassNames,
    ApiDocumentation? Documentation,
    ApiDocumentation? NewDocumentation,
    IReadOnlyList<string> DeclaredMemberNames)
{
    public InspectedClass ToInspectedClassWithBaseClassNames()
        => new(this.Name, this.Functions, this.HasStaticNew, BaseClassNames: this.BaseClassNames,
            Documentation: this.Documentation, NewDocumentation: this.NewDocumentation,
            DeclaredMemberNames: this.DeclaredMemberNames, HasMultipleBaseClasses: this.BaseClassNames.Count > 1);
}
