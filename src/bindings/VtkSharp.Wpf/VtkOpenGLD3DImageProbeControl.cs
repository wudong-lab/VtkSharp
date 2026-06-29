using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace VtkSharp.Wpf;

/// <summary>
/// Diagnostic probe that validates OpenGL-to-D3D9Ex interop without rendering VTK content.
/// </summary>
public sealed class VtkOpenGLD3DImageProbeControl : FrameworkElement, IDisposable
{
    private const string NativeLibraryName = "VtkSharp.Native";

    private readonly D3DImage _image = new();
    private nint _bridge;
    private nint _backBuffer;
    private PixelSize _pixelSize;
    private bool _isInitialized;
    private bool _isDisposed;

    public VtkOpenGLD3DImageProbeControl()
    {
        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged += this.OnIsFrontBufferAvailableChanged;
    }

    public bool Animate { get; set; } = true;

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

            VtkWpfOpenGLD3DImageProbeBridge_SetSize(this._bridge, pixelSize.Width, pixelSize.Height);
            this._pixelSize = pixelSize;

            var backBuffer = VtkWpfOpenGLD3DImageProbeBridge_GetBackBuffer(this._bridge);
            if (backBuffer == IntPtr.Zero) return;

            if (backBuffer != this._backBuffer)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer);
                this._backBuffer = backBuffer;
            }

            VtkWpfOpenGLD3DImageProbeBridge_Render(this._bridge);
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
        CompositionTarget.Rendering += this.OnCompositionTargetRendering;
        this.Render();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= this.OnCompositionTargetRendering;
        this.DisposeBridge();
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        if (this.Animate)
        {
            this.Render();
        }
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

        this._bridge = VtkWpfOpenGLD3DImageProbeBridge_New();
        if (this._bridge == IntPtr.Zero)
        {
            var nativeError = Marshal.PtrToStringAnsi(VtkWpfOpenGLD3DImageProbeBridge_GetLastError());
            var detail = string.IsNullOrWhiteSpace(nativeError)
                ? "The current GPU/driver may not support WGL_NV_DX_interop."
                : nativeError;

            throw new InvalidOperationException($"Failed to create OpenGL/D3DImage probe bridge. {detail}");
        }

        this._isInitialized = true;
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

            VtkWpfOpenGLD3DImageProbeBridge_Delete(this._bridge);
            this._bridge = IntPtr.Zero;
        }

        this._backBuffer = IntPtr.Zero;
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
    private static extern nint VtkWpfOpenGLD3DImageProbeBridge_New();

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfOpenGLD3DImageProbeBridge_Delete(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfOpenGLD3DImageProbeBridge_SetSize(nint bridge, int width, int height);

    [DllImport(NativeLibraryName)]
    private static extern void VtkWpfOpenGLD3DImageProbeBridge_Render(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkWpfOpenGLD3DImageProbeBridge_GetBackBuffer(nint bridge);

    [DllImport(NativeLibraryName)]
    private static extern nint VtkWpfOpenGLD3DImageProbeBridge_GetLastError();

    private readonly record struct PixelSize(int Width, int Height);
}
