using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;

namespace VtkSharp.Wpf;

/// <summary>
/// Recommended WPF VTK control that renders through OpenGL/D3D9Ex interop into a WPF D3DImage.
/// </summary>
public sealed class VtkOpenGlD3DImageRenderControl : FrameworkElement, IDisposable
{
    private readonly D3DImage _image = new();

    private VtkOpenGlD3DImageRender? _render;
    private vtkInteractorStyleTrackballCamera? _interactorStyle;

    private nint _backBuffer;
    private PixelSize _pixelSize;
    private bool _isInitialized;
    private bool _isDisposed;
    private Point? _lastMousePosition;

    public VtkOpenGlD3DImageRenderControl()
    {
        this.Focusable = true;
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged += this.OnIsFrontBufferAvailableChanged;
    }

    public vtkRenderWindow? RenderWindow { get; private set; }
    public vtkRenderer? Renderer { get; private set; }
    public vtkWin32RenderWindowInteractor? Interactor { get; private set; }

    public event EventHandler<VtkRenderControlInitializedEventArgs>? VtkInitialized;

    public void Render()
    {
        if (!this._isInitialized || this._render is null || !this._image.IsFrontBufferAvailable) return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));

        this._image.Lock();
        try
        {
            if (pixelSize != this._pixelSize && this._backBuffer != IntPtr.Zero)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                this._backBuffer = IntPtr.Zero;
            }

            if (!this._render.SetSize(pixelSize.Width, pixelSize.Height)) return;

            this._pixelSize = pixelSize;

            var backBuffer = this._render.GetBackBuffer();
            if (backBuffer == IntPtr.Zero) return;

            if (backBuffer != this._backBuffer)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer);
                this._backBuffer = backBuffer;
            }

            if (!this._render.Render()) return;

            this._image.AddDirtyRect(new Int32Rect(0, 0, pixelSize.Width, pixelSize.Height));
        }
        finally
        {
            this._image.Unlock();
        }

        this.InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawImage(this._image, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        this.Render();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            this.Focus();
            this.CaptureMouse();
            this._lastMousePosition = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (this._lastMousePosition is not { } lastPosition || e.LeftButton != MouseButtonState.Pressed || this.Renderer is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - lastPosition;
        this._lastMousePosition = currentPosition;

        var camera = this.Renderer.GetActiveCamera();
        camera.Azimuth(-delta.X * 0.5);
        camera.Elevation(delta.Y * 0.5);
        camera.ComputeViewPlaneNormal();
        this.Renderer.ResetCameraClippingRange();
        this.Render();
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Left)
        {
            this._lastMousePosition = null;
            this.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (this.Renderer is null) return;

        var camera = this.Renderer.GetActiveCamera();
        camera.Zoom(e.Delta > 0 ? 1.1 : 0.9);
        this.Renderer.ResetCameraClippingRange();
        this.Render();
        e.Handled = true;
    }

    public void Dispose()
    {
        if (this._isDisposed) return;

        this.DisposeVtkRender();
        this._isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.InitializeVtkRender();
        this.Render();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.DisposeVtkRender();
    }

    private void OnIsFrontBufferAvailableChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (this._image.IsFrontBufferAvailable)
        {
            this._backBuffer = IntPtr.Zero;
            this.Render();
        }
    }

    private void InitializeVtkRender()
    {
        if (this._isInitialized) return;

        this._render = VtkOpenGlD3DImageRender.Create();
        this.RenderWindow = this._render.RenderWindow;
        this.Renderer = this._render.Renderer;

        this.Interactor = vtkWin32RenderWindowInteractor.New();
        this.Interactor.SetRenderWindow(this.RenderWindow);

        this._interactorStyle = vtkInteractorStyleTrackballCamera.New();
        this.Interactor.SetInteractorStyle(this._interactorStyle);
        this.Interactor.Initialize();

        this._isInitialized = true;

        this.VtkInitialized?.Invoke(this, new VtkRenderControlInitializedEventArgs(this.RenderWindow, this.Renderer));
    }

    private void DisposeVtkRender()
    {
        if (this._render is not null)
        {
            if (this._image.IsFrontBufferAvailable)
            {
                this._image.Lock();
                try
                {
                    this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                }
                finally
                {
                    this._image.Unlock();
                }
            }

            this._render.Dispose();
            this._render = null;
        }

        this._backBuffer = IntPtr.Zero;
        this.RenderWindow = null;
        this.Renderer = null;
        this._isInitialized = false;
    }

    private PixelSize GetPixelSize(Size dipSize)
    {
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

        var width = Math.Max(1, (int)Math.Ceiling(dipSize.Width * transform.M11));
        var height = Math.Max(1, (int)Math.Ceiling(dipSize.Height * transform.M22));
        return new PixelSize(width, height);
    }

    private readonly record struct PixelSize(int Width, int Height);
}
