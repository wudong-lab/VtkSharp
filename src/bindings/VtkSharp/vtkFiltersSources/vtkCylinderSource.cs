namespace VtkSharp;

public partial class vtkCylinderSource
{
    public void SetCenter(VtkPoint3d center)
        => this.SetCenter(center.X, center.Y, center.Z);

    public unsafe VtkPoint3d GetCenter() 
        => VtkPoint3d.FromPointer(this.GetCenter_Internal());
}