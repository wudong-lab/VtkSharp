using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace VtkSharp.Wpf;

public sealed class VtkRenderHost : HwndHost
{
    private nint _hostHandle;
    private bool _isVtkInitialized;
    private vtkInteractorStyleTrackballCamera? _interactorStyle;

    public vtkWin32OpenGLRenderWindow? RenderWindow { get; private set; }
    public vtkRenderer? Renderer { get; private set; }
    public vtkWin32RenderWindowInteractor? Interactor { get; private set; }

    public event EventHandler<VtkRenderHostInitializedEventArgs>? VtkInitialized;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        this.InitializeVtk(hwndParent.Handle);
        this._hostHandle = this.RenderWindow?.GetWindowId() ?? nint.Zero;
        if (this._hostHandle == nint.Zero) throw new Win32Exception("VTK did not create a Win32 render window.");

        this.Dispatcher.BeginInvoke(this.UpdateHostSizeAndRender, DispatcherPriority.Loaded);
        return new HandleRef(this, this._hostHandle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        this.DisposeVtkObjects();
        this._hostHandle = nint.Zero;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (this._hostHandle == nint.Zero) return;

        this.UpdateHostSizeAndRender();
    }

    private void InitializeVtk(nint parentHandle)
    {
        if (this._isVtkInitialized) return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));

        this.Renderer = vtkRenderer.New();
        this.RenderWindow = vtkWin32OpenGLRenderWindow.New();
        this.RenderWindow.SetParentId(parentHandle);
        this.RenderWindow.ShowWindowOff();
        this.RenderWindow.SetSize(pixelSize.Width, pixelSize.Height);
        this.RenderWindow.AddRenderer(this.Renderer);

        this.Interactor = vtkWin32RenderWindowInteractor.New();
        this.Interactor.SetRenderWindow(this.RenderWindow);
        this._interactorStyle = vtkInteractorStyleTrackballCamera.New();
        this.Interactor.SetInteractorStyle(this._interactorStyle);
        this.Interactor.Initialize();

        this._isVtkInitialized = true;
        this.VtkInitialized?.Invoke(this, new VtkRenderHostInitializedEventArgs(this.RenderWindow, this.Renderer, this.Interactor));
        this.UpdateHostSizeAndRender();
    }

    private void UpdateHostSizeAndRender()
    {
        if (this._hostHandle == nint.Zero) return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));

        if (this.RenderWindow is not null)
        {
            this.RenderWindow.SetSize(pixelSize.Width, pixelSize.Height);
            this.RenderWindow.Render();
        }
    }

    private PixelSize GetPixelSize(Size dipSize)
    {
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

        return new PixelSize(
            Math.Max(1, (int)Math.Ceiling(dipSize.Width * transform.M11)),
            Math.Max(1, (int)Math.Ceiling(dipSize.Height * transform.M22)));
    }

    private void DisposeVtkObjects()
    {
        this.Interactor?.Dispose();
        this.Interactor = null;

        this._interactorStyle?.Dispose();
        this._interactorStyle = null;

        this.RenderWindow?.Dispose();
        this.RenderWindow = null;

        this.Renderer?.Dispose();
        this.Renderer = null;

        this._isVtkInitialized = false;
    }

    private readonly record struct PixelSize(int Width, int Height);
}