namespace VtkSharp.Wpf;

public sealed class VtkRenderControlInitializedEventArgs : EventArgs
{
    public VtkRenderControlInitializedEventArgs(vtkRenderWindow renderWindow, vtkRenderer renderer)
        : this(renderWindow, renderer, interactor: null)
    {
    }

    public VtkRenderControlInitializedEventArgs(
        vtkRenderWindow renderWindow,
        vtkRenderer renderer,
        vtkWin32RenderWindowInteractor? interactor)
    {
        this.RenderWindow = renderWindow;
        this.Renderer = renderer;
        this.Interactor = interactor;
    }

    public vtkRenderWindow RenderWindow { get; }
    public vtkRenderer Renderer { get; }
    public vtkWin32RenderWindowInteractor? Interactor { get; }
}
