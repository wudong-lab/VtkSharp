namespace VtkSharp;

public unsafe partial class vtkOpenGLRenderWindow
{
    public int GetRGBAPixelData(
        int x,
        int y,
        int x2,
        int y2,
        int front,
        vtkFloatArray data)
        => this.GetRGBAPixelData(x, y, x2, y2, front, data, 0);
}
