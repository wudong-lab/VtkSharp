using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VtkSharp.Wpf;

/// <summary>
/// Diagnostic fallback that renders VTK offscreen and copies frames into a WPF WriteableBitmap.
/// </summary>
public sealed class VtkNativeRenderControl : FrameworkElement, IDisposable
{
    private const string NativeLibraryName = "VtkSharp.Native";

    private nint _bridge;
    private WriteableBitmap? _bitmap;
    private byte[] _frameBuffer = Array.Empty<byte>();
    private bool _isInitialized;
    private bool _isDisposed;
    private Point? _lastMousePosition;

    public VtkNativeRenderControl()
    {
        this.Focusable = true;
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
    }

    public vtkRenderWindow? RenderWindow { get; private set; }
    public vtkRenderer? Renderer { get; private set; }

    public event EventHandler<VtkRenderControlInitializedEventArgs>? VtkInitialized;

    public void Render()
    {
        if (!this._isInitialized || this._bridge == IntPtr.Zero) return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));
        this.EnsureBackBuffer(pixelSize);

        VtkWpfCpuRenderBridge_SetSize(this._bridge, pixelSize.Width, pixelSize.Height);
        VtkWpfCpuRenderBridge_Render(this._bridge);

        if (VtkWpfCpuRenderBridge_CopyBgra(
                this._bridge,
                this._frameBuffer,
                this._frameBuffer.Length,
                out var width,
                out var height) == 0)
        {
            return;
        }

        if (this._bitmap is null || this._bitmap.PixelWidth != width || this._bitmap.PixelHeight != height)
        {
            this._bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }

        this._bitmap.WritePixels(
            new Int32Rect(0, 0, width, height),
            this._frameBuffer,
            width * 4,
            0);
        this.InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (this._bitmap is not null)
        {
            drawingContext.DrawImage(this._bitmap, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
        }
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

    private void InitializeBridge()
    {
        if (this._isInitialized) return;

        this._bridge = VtkWpfCpuRenderBridge_New();
        if (this._bridge == IntPtr.Zero) throw new InvalidOperationException("Failed to create VTK WPF render bridge.");

        this.RenderWindow = vtkRenderWindow.WeakReference(VtkWpfCpuRenderBridge_GetRenderWindow(this._bridge));
        this.Renderer = vtkRenderer.WeakReference(VtkWpfCpuRenderBridge_GetRenderer(this._bridge));
        this._isInitialized = true;

        this.VtkInitialized?.Invoke(this, new VtkRenderControlInitializedEventArgs(this.RenderWindow, this.Renderer));
    }

    private void DisposeBridge()
    {
        if (this._bridge != IntPtr.Zero)
        {
            VtkWpfCpuRenderBridge_Delete(this._bridge);
            this._bridge = IntPtr.Zero;
        }

        this.RenderWindow = null;
        this.Renderer = null;
        this._bitmap = null;
        this._frameBuffer = Array.Empty<byte>();
        this._isInitialized = false;
    }

    private void EnsureBackBuffer(PixelSize pixelSize)
    {
        var requiredLength = pixelSize.Width * pixelSize.Height * 4;
        if (this._frameBuffer.Length != requiredLength)
        {
            this._frameBuffer = new byte[requiredLength];
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

    [DllImport(NativeLibraryName)]
    private static extern nint VtkWpfCpuRenderBridge_New();

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfCpuRenderBridge_Delete(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkWpfCpuRenderBridge_GetRenderWindow(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkWpfCpuRenderBridge_GetRenderer(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfCpuRenderBridge_SetSize(nint bridge, int width, int height);

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfCpuRenderBridge_Render(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern int VtkWpfCpuRenderBridge_CopyBgra(
        nint bridge,
        byte[] destination,
        int destinationLength,
        out int width,
        out int height);

    private readonly record struct PixelSize(int Width, int Height);
}
