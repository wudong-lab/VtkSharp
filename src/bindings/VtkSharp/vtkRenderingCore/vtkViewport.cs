namespace VtkSharp;

public unsafe partial class vtkViewport
{
    public void SetBackground(VtkColor3d color)
        => this.SetBackground(color.R, color.G, color.B);

    public VtkColor3d GetBackground()
        => VtkColor3d.FromPointer(this.GetBackground_Internal());

    public void SetBackground2(VtkColor3d color)
        => this.SetBackground2(color.R, color.G, color.B);

    public VtkColor3d GetBackground2()
        => VtkColor3d.FromPointer(this.GetBackground2_Internal());
}