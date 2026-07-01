using System.Windows;
using System.Collections.Generic;
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
    private readonly Dictionary<int, VtkDispatcherTimer> _timers = new();

    private VtkOpenGlD3DImageRender? _render;
    private vtkInteractorStyleTrackballCamera? _interactorStyle;
    private VtkObserverHandle? _createTimerObserver;
    private VtkObserverHandle? _destroyTimerObserver;

    private nint _backBuffer;
    private PixelSize _pixelSize;
    private bool _isInitialized;
    private bool _isDisposed;
    private bool _renderRequested;
    private int _nextPlatformTimerId;
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
    public vtkRenderWindowInteractor? Interactor { get; private set; }

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

            var sizeChanged = pixelSize != this._pixelSize;
            if (!this._render.SetSize(pixelSize.Width, pixelSize.Height)) return;

            if (sizeChanged)
            {
                this.Interactor?.UpdateSize(pixelSize.Width, pixelSize.Height);
            }

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
        drawingContext.PushTransform(new MatrixTransform(1.0, 0.0, 0.0, -1.0, 0.0, this.ActualHeight));
        drawingContext.DrawImage(this._image, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
        drawingContext.Pop();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        this.RequestRender();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);

        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(vtkCommand.EnterEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(vtkCommand.LeaveEvent);
        this.RequestRender();
        e.Handled = true;
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
        this.InvokeInteractorEvent(vtkCommand.MouseMoveEvent);
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

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        if (this._activeMouseButton is not { } activeButton)
        {
            return;
        }

        this.InvokeMouseButtonEvent(activeButton, pressed: false, e.GetPosition(this), repeatCount: 0);
        this._activeMouseButton = null;
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(e.Delta > 0
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
        this.InvokeInteractorEvent(vtkCommand.KeyPressEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (this.Interactor is null) return;

        this.SetInteractorKeyEventInformation(e);
        this.InvokeInteractorEvent(vtkCommand.KeyReleaseEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);

        if (this.Interactor is null || string.IsNullOrEmpty(e.Text)) return;

        this.SetInteractorTextEventInformation(e.Text);
        this.InvokeInteractorEvent(vtkCommand.CharEvent);
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

        var interactor = vtkGenericRenderWindowInteractor.New();
        interactor.TimerEventResetsTimerOff();

        this.Interactor = interactor;
        this.Interactor.SetRenderWindow(this.RenderWindow);

        this._interactorStyle = vtkInteractorStyleTrackballCamera.New();
        this.Interactor.SetInteractorStyle(this._interactorStyle);
        this.Interactor.EnableRenderOff();
        this.AttachTimerBridge(this.Interactor);
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

        this.DetachTimerBridge();
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

    private void AttachTimerBridge(vtkRenderWindowInteractor interactor)
    {
        this._createTimerObserver = interactor.AddObserver(vtkCommand.CreateTimerEvent, (_, _) => this.CreatePlatformTimer());
        this._destroyTimerObserver = interactor.AddObserver(vtkCommand.DestroyTimerEvent, (_, _) => this.DestroyPlatformTimer());
    }

    private void DetachTimerBridge()
    {
        this._createTimerObserver?.Dispose();
        this._destroyTimerObserver?.Dispose();
        this._createTimerObserver = null;
        this._destroyTimerObserver = null;

        foreach (var timer in this._timers.Values)
        {
            timer.DispatcherTimer.Stop();
            timer.DispatcherTimer.Tick -= timer.OnTick;
        }

        this._timers.Clear();
    }

    private void CreatePlatformTimer()
    {
        if (this.Interactor is null) return;

        var timerId = this.Interactor.GetTimerEventId();
        var timerType = this.Interactor.GetTimerEventType();
        var duration = Math.Max(1, this.Interactor.GetTimerEventDuration());
        var platformTimerId = ++this._nextPlatformTimerId;

        EventHandler? onTick = null;
        var dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(duration)
        };

        onTick = (_, _) =>
        {
            if (this.Interactor is null) return;

            if (timerType == vtkRenderWindowInteractor.OneShotTimer)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Tick -= onTick;
                this._timers.Remove(platformTimerId);
            }

            this.Interactor.SetTimerEventId(timerId);
            if (this.Interactor is vtkGenericRenderWindowInteractor genericInteractor)
            {
                genericInteractor.TimerEvent();
            }
            else
            {
                this.Interactor.InvokeTimerEvent(timerId);
            }

            this.RequestRender();
        };

        dispatcherTimer.Tick += onTick;
        this._timers.Add(platformTimerId, new VtkDispatcherTimer(dispatcherTimer, onTick));
        this.Interactor.SetTimerEventPlatformId(platformTimerId);
        dispatcherTimer.Start();
    }

    private void DestroyPlatformTimer()
    {
        if (this.Interactor is null) return;

        var platformTimerId = this.Interactor.GetTimerEventPlatformId();
        if (!this._timers.TryGetValue(platformTimerId, out var timer)) return;

        this._timers.Remove(platformTimerId);
        timer.DispatcherTimer.Stop();
        timer.DispatcherTimer.Tick -= timer.OnTick;
    }

    private void InvokeMouseButtonEvent(MouseButton button, bool pressed, Point position, int repeatCount)
    {
        if (this.Interactor is null) return;

        this.SetInteractorEventInformation(position, repeatCount);
        this.InvokeInteractorEvent(GetMouseButtonEvent(button, pressed));
        this.RequestRender();
    }

    private void InvokeInteractorEvent(uint eventId)
    {
        if (this.Interactor is null) return;

        if (this.Interactor is vtkGenericRenderWindowInteractor genericInteractor)
        {
            InvokeGenericInteractorEvent(genericInteractor, eventId);
            return;
        }

        this.Interactor.InvokeEvent(eventId);
    }

    private static void InvokeGenericInteractorEvent(vtkGenericRenderWindowInteractor interactor, uint eventId)
    {
        switch (eventId)
        {
            case vtkCommand.MouseMoveEvent:
                interactor.MouseMoveEvent();
                break;
            case vtkCommand.LeftButtonPressEvent:
                interactor.LeftButtonPressEvent();
                break;
            case vtkCommand.LeftButtonReleaseEvent:
                interactor.LeftButtonReleaseEvent();
                break;
            case vtkCommand.MiddleButtonPressEvent:
                interactor.MiddleButtonPressEvent();
                break;
            case vtkCommand.MiddleButtonReleaseEvent:
                interactor.MiddleButtonReleaseEvent();
                break;
            case vtkCommand.RightButtonPressEvent:
                interactor.RightButtonPressEvent();
                break;
            case vtkCommand.RightButtonReleaseEvent:
                interactor.RightButtonReleaseEvent();
                break;
            case vtkCommand.MouseWheelForwardEvent:
                interactor.MouseWheelForwardEvent();
                break;
            case vtkCommand.MouseWheelBackwardEvent:
                interactor.MouseWheelBackwardEvent();
                break;
            case vtkCommand.EnterEvent:
                interactor.EnterEvent();
                break;
            case vtkCommand.LeaveEvent:
                interactor.LeaveEvent();
                break;
            case vtkCommand.KeyPressEvent:
                interactor.KeyPressEvent();
                break;
            case vtkCommand.KeyReleaseEvent:
                interactor.KeyReleaseEvent();
                break;
            case vtkCommand.CharEvent:
                interactor.CharEvent();
                break;
            default:
                interactor.InvokeEvent(eventId);
                break;
        }
    }

    private void SetInteractorEventInformation(Point position, int repeatCount)
    {
        if (this.Interactor is null) return;

        var pixelPosition = this.GetPixelPosition(position);
        var modifiers = Keyboard.Modifiers;
        this.Interactor.SetEventInformationFlipY(
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
            e.IsRepeat ? 1 : 0,
            GetKeySym(e));
        this.Interactor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private void SetInteractorTextEventInformation(string text)
    {
        if (this.Interactor is null || text.Length == 0) return;

        var modifiers = Keyboard.Modifiers;
        var keyCode = text[0] <= byte.MaxValue ? text[0] : '\0';
        var keySym = text.Length == 1 ? text : null;
        this.Interactor.SetKeyEventInformation(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            keyCode,
            repeatCount: 0,
            keySym);
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

    private static string? GetKeySym(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        return key switch
        {
            Key.A => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "A" : "a",
            Key.B => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "B" : "b",
            Key.C => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "C" : "c",
            Key.D => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "D" : "d",
            Key.E => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "E" : "e",
            Key.F => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "F" : "f",
            Key.G => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "G" : "g",
            Key.H => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "H" : "h",
            Key.I => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "I" : "i",
            Key.J => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "J" : "j",
            Key.K => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "K" : "k",
            Key.L => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "L" : "l",
            Key.M => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "M" : "m",
            Key.N => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "N" : "n",
            Key.O => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "O" : "o",
            Key.P => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "P" : "p",
            Key.Q => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Q" : "q",
            Key.R => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "R" : "r",
            Key.S => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "S" : "s",
            Key.T => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "T" : "t",
            Key.U => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "U" : "u",
            Key.V => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "V" : "v",
            Key.W => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "W" : "w",
            Key.X => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "X" : "x",
            Key.Y => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Y" : "y",
            Key.Z => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Z" : "z",
            Key.D0 or Key.NumPad0 => "0",
            Key.D1 or Key.NumPad1 => "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => "5",
            Key.D6 or Key.NumPad6 => "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 or Key.NumPad8 => "8",
            Key.D9 or Key.NumPad9 => "9",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "Prior",
            Key.PageDown => "Next",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Back => "BackSpace",
            Key.Tab => "Tab",
            Key.Enter or Key.Return => "Return",
            Key.Escape => "Escape",
            Key.Space => "space",
            Key.LeftShift or Key.RightShift => "Shift_L",
            Key.LeftCtrl or Key.RightCtrl => "Control_L",
            Key.LeftAlt or Key.RightAlt => "Alt_L",
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            Key.F6 => "F6",
            Key.F7 => "F7",
            Key.F8 => "F8",
            Key.F9 => "F9",
            Key.F10 => "F10",
            Key.F11 => "F11",
            Key.F12 => "F12",
            _ => null
        };
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
    private sealed record VtkDispatcherTimer(DispatcherTimer DispatcherTimer, EventHandler OnTick);
}
