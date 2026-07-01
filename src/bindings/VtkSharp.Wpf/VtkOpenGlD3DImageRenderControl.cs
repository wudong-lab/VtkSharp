using System.Windows;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Input;

namespace VtkSharp.Wpf;

/// <summary>
/// Recommended WPF VTK control that renders through OpenGL/D3D9Ex interop into a WPF D3DImage.
/// </summary>
public sealed partial class VtkOpenGlD3DImageRenderControl : FrameworkElement, IDisposable
{
    public static readonly DependencyProperty DisposeOnUnloadProperty = DependencyProperty.Register(
        nameof(DisposeOnUnload),
        typeof(bool),
        typeof(VtkOpenGlD3DImageRenderControl),
        new PropertyMetadata(true));

    private readonly D3DImage _image = new();
    private readonly Dictionary<int, VtkDispatcherTimer> _timers = new();

    private VtkOpenGlD3DImageRender? _render;
    private vtkInteractorStyleTrackballCamera? _interactorStyle;
    private VtkObserverHandle? _createTimerObserver;
    private VtkObserverHandle? _destroyTimerObserver;
    private VtkObserverHandle? _cursorChangedObserver;

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

    public bool DisposeOnUnload
    {
        get => (bool)this.GetValue(DisposeOnUnloadProperty);
        set => this.SetValue(DisposeOnUnloadProperty, value);
    }

    public event EventHandler<VtkRenderControlInitializedEventArgs>? VtkInitialized;
    public event EventHandler<VtkRenderFailedEventArgs>? RenderFailed;

}
