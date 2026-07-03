using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace VtkSharp.Wpf;

public sealed partial class VtkOpenGlD3DImageRenderControl
{
    public void RequestRender()
    {
        if (this._isDisposed || this._renderRequested || this._isInDesignMode) return;

        this._renderRequested = true;
        this.Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(() =>
            {
                this._renderRequested = false;
                this.Render();
            }));
    }

    public void Render()
    {
        if (this._isDisposed ||
            !this.IsLoaded ||
            !this.IsVtkRenderInitialized ||
            this._render is null ||
            !this._image.IsFrontBufferAvailable ||
            this.ActualWidth <= 0 ||
            this.ActualHeight <= 0)
            return;

        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));
        string? renderFailure = null;

        this._image.Lock();
        try
        {
            if (pixelSize != this._pixelSize && this._backBuffer != IntPtr.Zero)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                this._backBuffer = IntPtr.Zero;
            }

            var sizeChanged = pixelSize != this._pixelSize;
            if (!this._render.SetSize(pixelSize.Width, pixelSize.Height))
            {
                renderFailure = this.GetRenderError("Failed to resize the VTK D3DImage render target.");
                return;
            }

            if (sizeChanged)
            {
                this.RenderWindowInteractor?.UpdateSize(pixelSize.Width, pixelSize.Height);
            }

            this._pixelSize = pixelSize;

            var backBuffer = this._render.GetBackBuffer();
            if (backBuffer == IntPtr.Zero)
            {
                renderFailure = this.GetRenderError("The VTK D3DImage render target did not provide a back buffer.");
                return;
            }

            if (backBuffer != this._backBuffer)
            {
                this._image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer);
                this._backBuffer = backBuffer;
            }

            if (!this._render.Render())
            {
                renderFailure = this.GetRenderError("Failed to render the VTK scene.");
                return;
            }

            this._image.AddDirtyRect(new Int32Rect(0, 0, pixelSize.Width, pixelSize.Height));
        }
        finally
        {
            this._image.Unlock();
            if (renderFailure is not null)
            {
                this.OnRenderFailed(renderFailure);
            }
        }

        this.InvalidateVisual();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        this.RequestRender();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (this._isInDesignMode)
        {
            DesignTimeHelper.DrawDesignTimePlaceholder(drawingContext, this.ActualWidth, this.ActualHeight);
            return;
        }

        drawingContext.PushTransform(new MatrixTransform(1.0, 0.0, 0.0, -1.0, 0.0, this.ActualHeight));
        drawingContext.DrawImage(this._image, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
        drawingContext.Pop();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        this.RequestRender();
    }

    private string GetRenderError(string fallbackMessage)
    {
        var nativeError = this._render?.GetLastError();
        return string.IsNullOrWhiteSpace(nativeError) ? fallbackMessage : nativeError!;
    }

    private void OnRenderFailed(string message)
    {
        this.VtkRenderFailed?.Invoke(this, new VtkRenderFailedEventArgs(message));
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