using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace VtkSharp.Wpf;

public sealed partial class VtkOpenGlD3DImageRenderControl
{
    public void Dispose()
    {
        if (this._isDisposed) return;

        this.DisposeVtkRender();
        this.Loaded -= this.OnLoaded;
        this.Unloaded -= this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged -= this.OnIsFrontBufferAvailableChanged;
        this._isDisposed = true;

        //
        GC.SuppressFinalize(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Debug.Assert(!this._isInDesignMode);

        this.InitializeVtkRender();
        this.RequestRender();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Debug.Assert(!this._isInDesignMode);

        if (this.DisposeOnUnload)
        {
            this.DisposeVtkRender();
            return;
        }

        this.SuspendVtkRender();
    }

    private void OnIsFrontBufferAvailableChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (this._image.IsFrontBufferAvailable)
        {
            this._backBuffer = IntPtr.Zero;
            this.RequestRender();
        }
    }

    private void InitializeVtkRender()
    {
        if (this.IsVtkRenderInitialized) return;

        this._render = VtkOpenGlD3DImageRender.Create();
        this.RenderWindow = this._render.RenderWindow;
        this.Renderer = this._render.Renderer;
        this.AttachCursorObserver(this.RenderWindow);

        var interactor = vtkGenericRenderWindowInteractor.New();
        interactor.TimerEventResetsTimerOff();

        this.RenderWindowInteractor = interactor;
        this.RenderWindowInteractor.SetRenderWindow(this.RenderWindow);

        this._interactorStyle = vtkInteractorStyleTrackballCamera.New();
        this.RenderWindowInteractor.SetInteractorStyle(this._interactorStyle);
        this.RenderWindowInteractor.EnableRenderOff();
        this.AttachTimerObservers(this.RenderWindowInteractor);
        this.RenderWindowInteractor.Initialize();

        this.IsVtkRenderInitialized = true;

        //
        this.VtkRenderInitialized?.Invoke(this, new VtkRenderInitializedEventArgs(this.RenderWindow, this.Renderer, this.RenderWindowInteractor));
    }

    private void DisposeVtkRender()
    {
        if (!this.IsVtkRenderInitialized) return;

        this.VtkRenderDisposing?.Invoke(this, new VtkRenderDisposingEventArgs(this.RenderWindow!, this.Renderer!, this.RenderWindowInteractor!));

        //
        this.ReleaseActiveMouseInteraction();

        this.DetachTimerObservers();
        this.DetachCursorObserver();
        this.RenderWindowInteractor?.Dispose();
        this._interactorStyle?.Dispose();
        this.RenderWindowInteractor = null;
        this._interactorStyle = null;

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
        this.Cursor = null;
        this.RenderWindow = null;
        this.Renderer = null;

        //
        this.IsVtkRenderInitialized = false;
    }

    private void SuspendVtkRender()
    {
        this.ReleaseActiveMouseInteraction();
        this.StopPlatformTimers();
        this._renderRequested = false;
    }

    private void ReleaseActiveMouseInteraction()
    {
        if (this._activeMouseButton is { } activeButton)
        {
            this.InvokeMouseButtonEvent(activeButton, pressed: false, Mouse.GetPosition(this), repeatCount: 0);
            this._activeMouseButton = null;
        }

        if (this.IsMouseCaptured)
        {
            this.ReleaseMouseCapture();
        }
    }
}