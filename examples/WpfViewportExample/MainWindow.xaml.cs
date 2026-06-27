using System.Windows;
using VtkSharp.Wpf;

namespace VtkSharp.WpfViewportExample;

public partial class MainWindow : Window
{
    private vtkConeSource? _cone;
    private vtkPolyDataMapper? _mapper;
    private vtkActor? _actor;

    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    private void OnVtkInitialized(object? sender, VtkRenderHostInitializedEventArgs e)
    {
        _cone = vtkConeSource.New();
        _cone.SetHeight(3.0);
        _cone.SetRadius(1.0);
        _cone.SetResolution(32);

        _mapper = vtkPolyDataMapper.New();
        _mapper.SetInputConnection(_cone.GetOutputPort());

        _actor = vtkActor.New();
        _actor.SetMapper(_mapper);
        _actor.GetProperty().SetColor(0.9, 0.42, 0.22);

        e.Renderer.SetBackground(0.12, 0.14, 0.18);
        e.Renderer.AddActor(_actor);
        e.Renderer.ResetCamera();
        e.RenderWindow.Render();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _actor?.Dispose();
        _mapper?.Dispose();
        _cone?.Dispose();
    }
}
