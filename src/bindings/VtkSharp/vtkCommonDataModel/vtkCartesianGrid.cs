namespace VtkSharp;

public unsafe partial class vtkCartesianGrid
{
    public VtkDimension GetDimensions()
    {
        return VtkDimension.FromPointer(this.GetDimensions_Internal());
    }
}