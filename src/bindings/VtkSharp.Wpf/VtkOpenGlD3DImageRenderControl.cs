using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
    private bool _renderRequested;
    private MouseButton? _activeMouseButton;

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

    public void RequestRender()
    {
        if (this._isDisposed || this._renderRequested) return;

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
            !this._isInitialized ||
            this._render is null ||
            !this._image.IsFrontBufferAvailable ||
            this.ActualWidth <= 0 ||
            this.ActualHeight <= 0)
        {
            return;
        }

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
        this.RequestRender();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
        {
            this.Focus();
            this.CaptureMouse();
            this._activeMouseButton = e.ChangedButton;
            this.InvokeMouseButtonEvent(e.ChangedButton, pressed: true, e.GetPosition(this), e.ClickCount);
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (this.Interactor is null)
        {
            return;
        }

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.Interactor.InvokeEvent(vtkCommand.MouseMoveEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == this._activeMouseButton)
        {
            this.InvokeMouseButtonEvent(e.ChangedButton, pressed: false, e.GetPosition(this), repeatCount: 0);
            this._activeMouseButton = null;
            this.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.Interactor.InvokeEvent(e.Delta > 0
            ? vtkCommand.MouseWheelForwardEvent
            : vtkCommand.MouseWheelBackwardEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (this.Interactor is null) return;

        this.SetInteractorKeyEventInformation(e);
        this.Interactor.InvokeEvent(vtkCommand.KeyPressEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (this.Interactor is null) return;

        this.SetInteractorKeyEventInformation(e);
        this.Interactor.InvokeEvent(vtkCommand.KeyReleaseEvent);
        this.RequestRender();
        e.Handled = true;
    }

    public void Dispose()
    {
        if (this._isDisposed) return;

        this.DisposeVtkRender();
        this.Loaded -= this.OnLoaded;
        this.Unloaded -= this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged -= this.OnIsFrontBufferAvailableChanged;
        this._isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.InitializeVtkRender();
        this.RequestRender();
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
            this.RequestRender();
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
        this.Interactor.EnableRenderOff();
        this.Interactor.Initialize();

        this._isInitialized = true;

        this.VtkInitialized?.Invoke(
            this,
            new VtkRenderControlInitializedEventArgs(this.RenderWindow, this.Renderer, this.Interactor));
    }

    private void DisposeVtkRender()
    {
        this._activeMouseButton = null;
        if (this.IsMouseCaptured)
        {
            this.ReleaseMouseCapture();
        }

        this.Interactor?.Dispose();
        this._interactorStyle?.Dispose();
        this.Interactor = null;
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
        this.RenderWindow = null;
        this.Renderer = null;
        this._isInitialized = false;
    }

    private void InvokeMouseButtonEvent(MouseButton button, bool pressed, Point position, int repeatCount)
    {
        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(position, repeatCount);
        this.Interactor.InvokeEvent(GetMouseButtonEvent(button, pressed));
        this.RequestRender();
    }

    private void SetInteractorEventInformation(Point position, int repeatCount)
    {
        if (this.Interactor is null) return;

        var pixelPosition = this.GetPixelPosition(position);
        var modifiers = Keyboard.Modifiers;
        this.Interactor.SetEventInformation(
            pixelPosition.X,
            pixelPosition.Y,
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            keyCode: '\0',
            repeatCount);
        this.Interactor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private void SetInteractorKeyEventInformation(KeyEventArgs e)
    {
        if (this.Interactor is null) return;

        var modifiers = Keyboard.Modifiers;
        this.Interactor.SetKeyEventInformation(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            GetKeyCode(e),
            e.IsRepeat ? 1 : 0);
        this.Interactor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private PixelPoint GetPixelPosition(Point position)
    {
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

        var x = (int)Math.Round(position.X * transform.M11);
        var y = (int)Math.Round(position.Y * transform.M22);
        return new PixelPoint(x, y);
    }

    private static uint GetMouseButtonEvent(MouseButton button, bool pressed)
    {
        return button switch
        {
            MouseButton.Left => pressed ? vtkCommand.LeftButtonPressEvent : vtkCommand.LeftButtonReleaseEvent,
            MouseButton.Middle => pressed ? vtkCommand.MiddleButtonPressEvent : vtkCommand.MiddleButtonReleaseEvent,
            MouseButton.Right => pressed ? vtkCommand.RightButtonPressEvent : vtkCommand.RightButtonReleaseEvent,
            _ => vtkCommand.NoEvent
        };
    }

    private static char GetKeyCode(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        return virtualKey is >= 0 and <= byte.MaxValue ? (char)virtualKey : '\0';
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
    private readonly record struct PixelPoint(int X, int Y);
}
