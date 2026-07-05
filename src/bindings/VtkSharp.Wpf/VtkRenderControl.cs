using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using VtkSharp.Wpf.Interop;

namespace VtkSharp.Wpf;

/// <summary>
/// Recommended WPF VTK control that renders through OpenGL/D3D9Ex interop into a WPF D3DImage.
/// </summary>
public sealed partial class VtkRenderControl : FrameworkElement, IDisposable
{
    private readonly D3DImage _image = new();
    private readonly Dictionary<int, VtkDispatcherTimer> _timers = new();

    private VtkOpenGlD3DImageRender? _render;
    private vtkInteractorStyleTrackballCamera? _interactorStyle;
    private VtkObserverHandle? _createTimerObserver;
    private VtkObserverHandle? _destroyTimerObserver;
    private VtkObserverHandle? _cursorChangedObserver;

    private nint _backBuffer;
    private PixelSize _pixelSize;
    private bool _isDisposed;
    private bool _renderRequested;
    private int _nextPlatformTimerId;
    private MouseButton? _activeMouseButton;
    private readonly bool _isInDesignMode = false;

    public VtkRenderControl()
    {
        this._isInDesignMode = DesignerProperties.GetIsInDesignMode(this);
        if (this._isInDesignMode) return;

        this.Focusable = true;

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
        this._image.IsFrontBufferAvailableChanged += this.OnIsFrontBufferAvailableChanged;
    }

    public vtkRenderWindow? RenderWindow { get; private set; }
    public vtkRenderer? Renderer { get; private set; }
    public vtkRenderWindowInteractor? RenderWindowInteractor { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the VTK render has been initialized.
    /// </summary>
    public bool IsVtkRenderInitialized { get; private set; }

    public static readonly DependencyProperty DisposeOnUnloadProperty = DependencyProperty.Register(
        nameof(DisposeOnUnload), typeof(bool), typeof(VtkRenderControl), new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value indicating whether the VTK render should be disposed when the control is unloaded.
    /// </summary>
    public bool DisposeOnUnload
    {
        get => (bool)this.GetValue(DisposeOnUnloadProperty);
        set => this.SetValue(DisposeOnUnloadProperty, value);
    }

    public event EventHandler<VtkRenderInitializedEventArgs>? VtkRenderInitialized;
    public event EventHandler<VtkRenderDisposingEventArgs>? VtkRenderDisposing;
    public event EventHandler<VtkRenderFailedEventArgs>? VtkRenderFailed;
}