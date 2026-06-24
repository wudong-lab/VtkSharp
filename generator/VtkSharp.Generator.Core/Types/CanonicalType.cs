namespace VtkSharp.Generator.Core.Types;

public readonly record struct CanonicalType(string Text)
{
    public override string ToString() => this.Text;
}
