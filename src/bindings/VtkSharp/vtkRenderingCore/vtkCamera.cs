namespace VtkSharp;

public unsafe partial class vtkCamera
{
    public void SetPosition(VtkPoint3d position)
        => this.SetPosition(position.X, position.Y, position.Z);

    public VtkPoint3d GetPosition()
        => VtkPoint3d.FromPointer(this.GetPosition_Internal());

    public void SetFocalPoint(VtkPoint3d focalPoint)
        => this.SetFocalPoint(focalPoint.X, focalPoint.Y, focalPoint.Z);

    public VtkPoint3d GetFocalPoint()
        => VtkPoint3d.FromPointer(this.GetFocalPoint_Internal());

    public void SetViewUp(VtkVector3d viewUp)
        => this.SetViewUp(viewUp.X, viewUp.Y, viewUp.Z);

    public VtkVector3d GetViewUp()
        => VtkVector3d.FromPointer(this.GetViewUp_Internal());
}