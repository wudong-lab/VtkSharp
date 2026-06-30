using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;

namespace VtkSharp.Wpf;

/// <summary>
/// Recommended WPF VTK control that renders through OpenGL/D3D9Ex interop into a WPF D3DImage.
/// </summary>
public sealed class VtkOpenGLD3DImageRenderControl : FrameworkElement, IDisposable
{
    private const string NativeLibraryName = "VtkSharp.Native";

    private readonly D3DImage _image = new();
    private nint _bridge;
    private nint _backBuffer;
    private PixelSize _pixelSize;
    private bool _isInitialized;
    private bool _isDisposed;
    private Point? _lastMousePosition;

    public VtkOpenGLD3DImageRenderControl()
    {
        this.Focusable = true;
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged += this.OnIsFrontBufferAvailableChanged;
    }

    public vtkRenderWindow? RenderWindow { get; private set; }
    public vtkRenderer? Renderer { get; private set; }

    public event EventHandler<VtkRenderControlInitializedEventArgs>? VtkInitialized;

    public void Render()
    {
        if (!this._isInitialized || this._bridge == IntPtr.Zero || !this._image.IsFrontBufferAvailable) return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));

        this._image.Lock();
        try
        {
            if (pixelSize != this._pixelSize && this._backBuffer != IntPtr.Zero)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                this._backBuffer = IntPtr.Zero;
            }

            VtkOpenGlD3DImageRender_SetSize(this._bridge, pixelSize.Width, pixelSize.Height);
            this._pixelSize = pixelSize;

            var backBuffer = VtkOpenGlD3DImageRender_GetBackBuffer(this._bridge);
            if (backBuffer == IntPtr.Zero) return;

            if (backBuffer != this._backBuffer)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer);
                this._backBuffer = backBuffer;
            }

            VtkOpenGlD3DImageRender_Render(this._bridge);
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

        this.DisposeBridge();
        this._isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.InitializeBridge();
        this.Render();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.DisposeBridge();
    }

    private void OnIsFrontBufferAvailableChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (this._image.IsFrontBufferAvailable)
        {
            this._backBuffer = IntPtr.Zero;
            this.Render();
        }
    }

    private void InitializeBridge()
    {
        if (this._isInitialized) return;

        this._bridge = VtkOpenGlD3DImageRender_New();
        if (this._bridge == IntPtr.Zero)
        {
            var nativeError = Marshal.PtrToStringAnsi(VtkOpenGlD3DImageRender_GetLastError());
            var detail = string.IsNullOrWhiteSpace(nativeError)
                ? "The current GPU/driver may not support WGL_NV_DX_interop."
                : nativeError;

            throw new InvalidOperationException($"Failed to create VTK OpenGL/D3DImage render bridge. {detail}");
        }

        this.RenderWindow = vtkRenderWindow.WeakReference(VtkOpenGlD3DImageRender_GetRenderWindow(this._bridge));
        this.Renderer = vtkRenderer.WeakReference(VtkOpenGlD3DImageRender_GetRenderer(this._bridge));
        this._isInitialized = true;

        this.VtkInitialized?.Invoke(this, new VtkRenderControlInitializedEventArgs(this.RenderWindow, this.Renderer));
    }

    private void DisposeBridge()
    {
        if (this._bridge != IntPtr.Zero)
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

            VtkOpenGlD3DImageRender_Delete(this._bridge);
            this._bridge = IntPtr.Zero;
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

        return new PixelSize(
            Math.Max(1, (int)Math.Ceiling(dipSize.Width * transform.M11)),
            Math.Max(1, (int)Math.Ceiling(dipSize.Height * transform.M22)));
    }

    [DllImport(NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_New();

    [DllImport(NativeLibraryName)]
    private static extern void VtkOpenGlD3DImageRender_Delete(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetRenderWindow(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetRenderer(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern void VtkOpenGlD3DImageRender_SetSize(nint bridge, int width, int height);

    [DllImport(NativeLibraryName)]
    private static extern void VtkOpenGlD3DImageRender_Render(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetBackBuffer(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetLastError();

    private readonly record struct PixelSize(int Width, int Height);
}
