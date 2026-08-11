using System.Windows;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("WPF HwndHost", "ExtraExamples",
    Description = "Hosts a native VTK Win32 render window in WPF. This approach has WPF airspace limitations.",
    SourceFiles = new[]
    {
        "ExtraExamples/WpfHwndHost/WpfHwndHost.cs",
        "ExtraExamples/WpfHwndHost/VtkRenderHost.cs"
    })]
internal sealed class WpfHwndHost : IExample
{
    public void Run()
    {
        var dispatcher = Application.Current?.Dispatcher
                         ?? throw new InvalidOperationException("WPF application dispatcher is not available.");

        dispatcher.Invoke(() =>
        {
            var window = new VtkRenderHostDemoWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        });
    }

    private sealed class VtkRenderHostDemoWindow : Window
    {
        private vtkConeSource? _cone;
        private vtkPolyDataMapper? _mapper;
        private vtkActor? _actor;

        public VtkRenderHostDemoWindow()
        {
            this.Title = "VtkSharp WPF HwndHost";
            this.Width = 1000;
            this.Height = 720;
            this.MinWidth = 480;
            this.MinHeight = 360;

            var host = new VtkRenderHost();
            host.VtkRenderHostInitialized += this.OnVtkInitialized;
            this.Content = host;

            this.Closed += this.OnClosed;
        }

        private void OnVtkInitialized(object? sender, VtkRenderHostInitializedEventArgs e)
        {
            this._cone = vtkConeSource.New();
            this._cone.SetHeight(3.0);
            this._cone.SetRadius(1.0);
            this._cone.SetResolution(32);

            this._mapper = vtkPolyDataMapper.New();
            this._mapper.SetInputConnection(this._cone.GetOutputPort());

            this._actor = vtkActor.New();
            this._actor.SetMapper(this._mapper);
            this._actor.GetProperty().SetColor(0.9, 0.42, 0.22);

            e.Renderer.SetBackground(0.12, 0.14, 0.18);
            e.Renderer.AddActor(this._actor);
            e.Renderer.ResetCamera();
            e.RenderWindow.Render();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            this._actor?.Dispose();
            this._mapper?.Dispose();
            this._cone?.Dispose();
        }
    }
}
