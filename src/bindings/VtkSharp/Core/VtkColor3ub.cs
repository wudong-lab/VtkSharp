namespace VtkSharp;

/// <summary>
/// VTK 颜色值，对应 <c>vtkColor3ub</c>（RGB，分量范围 0–255）。
/// </summary>
public readonly struct VtkColor3ub
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public VtkColor3ub(byte r, byte g, byte b)
    {
        this.R = r;
        this.G = g;
        this.B = b;
    }
}
