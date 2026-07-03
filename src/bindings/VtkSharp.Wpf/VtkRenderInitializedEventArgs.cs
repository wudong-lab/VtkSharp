namespace VtkSharp.Wpf;

public sealed class VtkRenderInitializedEventArgs : EventArgs
{
    public VtkRenderInitializedEventArgs(
        vtkRenderWindow renderWindow, 
        vtkRenderer renderer, 
        vtkRenderWindowInteractor renderWindowInteractor)
    {
        this.RenderWindow = renderWindow;
        this.Renderer = renderer;
        this.RenderWindowInteractor = renderWindowInteractor;
    }

    public vtkRenderWindow RenderWindow { get; }
    public vtkRenderer Renderer { get; }
    public vtkRenderWindowInteractor RenderWindowInteractor { get; }
}