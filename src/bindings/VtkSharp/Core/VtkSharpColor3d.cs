namespace VtkSharp;

/// <summary>
/// VTK颜色值，对应<see cref="vtkColor3d"/>
/// </summary>
public readonly struct VtkSharpColor3d
{
    public VtkSharpColor3d(double r, double g, double b)
    {
        this.R = r;
        this.G = g;
        this.B = b;
    }

    public double R { get; }
    public double G { get; }
    public double B { get; }
}