namespace VtkSharp.Wpf;

public sealed class VtkRenderHostInitializedEventArgs : EventArgs
{
    public VtkRenderHostInitializedEventArgs(
        vtkRenderWindow renderWindow,
        vtkRenderer renderer,
        vtkRenderWindowInteractor interactor)
    {
        this.RenderWindow = renderWindow;
        this.Renderer = renderer;
        this.Interactor = interactor;
    }

    public vtkRenderWindow RenderWindow { get; }
    public vtkRenderer Renderer { get; }
    public vtkRenderWindowInteractor Interactor { get; }
}
