namespace VtkSharp.Wpf;

public sealed class VtkRenderControlInitializedEventArgs : EventArgs
{
    public VtkRenderControlInitializedEventArgs(vtkRenderWindow renderWindow, vtkRenderer renderer)
    {
        this.RenderWindow = renderWindow;
        this.Renderer = renderer;
    }

    public vtkRenderWindow RenderWindow { get; }
    public vtkRenderer Renderer { get; }
}
