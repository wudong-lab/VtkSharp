namespace VtkSharp;

public partial class vtkProperty
{
    public void SetColor(VtkColor3d color)
        => this.SetColor(color.R, color.G, color.B);

    public void SetEdgeColor(VtkColor3d color)
        => this.SetEdgeColor(color.R, color.G, color.B);

    public void SetDiffuseColor(VtkColor3d color)
        => this.SetDiffuseColor(color.R, color.G, color.B);
}