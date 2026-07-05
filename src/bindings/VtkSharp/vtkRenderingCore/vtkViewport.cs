namespace VtkSharp;

public partial class vtkViewport
{
    public new void SetBackground(VtkColor3d color)
    {
        this.SetBackground(color.R, color.G, color.B);
    }

    public new void SetBackground2(VtkColor3d color)
    {
        this.SetBackground2(color.R, color.G, color.B);
    }
}