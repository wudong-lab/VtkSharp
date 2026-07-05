namespace VtkSharp;

public partial class vtkProperty
{
    public void SetColor(VtkColor3d color)
    {
        this.SetColor(color.R, color.G, color.B);
    }
}