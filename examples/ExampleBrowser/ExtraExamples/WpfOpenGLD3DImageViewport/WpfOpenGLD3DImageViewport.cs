using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VtkSharp.Wpf;

namespace VtkSharp.ExampleBrowser.ExtraExamples;

[Example("WPF VTK D3DImage Viewport", "WPF",
    Description = "Recommended WPF VTK viewport using OpenGL/D3D9Ex interop and D3DImage.",
    SourceFiles = new[] { "ExtraExamples/WpfOpenGLD3DImageViewport/WpfOpenGLD3DImageViewport.cs" })]
internal sealed class WpfOpenGLD3DImageViewport : IExample
{
    public void Run()
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF application dispatcher is not available.");

        dispatcher.Invoke(() =>
        {
            var window = new WpfOpenGLD3DImageViewportWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        });
    }

    private sealed class WpfOpenGLD3DImageViewportWindow : Window
    {
        private vtkConeSource? _cone;
        private vtkPolyDataMapper? _mapper;
        private vtkActor? _actor;

        public WpfOpenGLD3DImageViewportWindow()
        {
            this.Title = "VtkSharp WPF OpenGL D3DImage Viewport";
            this.Width = 1000;
            this.Height = 720;
            this.MinWidth = 480;
            this.MinHeight = 360;

            var viewport = new VtkOpenGlD3DImageRenderControl();
            viewport.VtkInitialized += this.OnVtkInitialized;

            var overlayButton = new Button
            {
                Content = "OpenGL/D3D VTK",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(16),
                Padding = new Thickness(14, 8, 14, 8),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };

            var root = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 10, 13))
            };
            root.Children.Add(viewport);
            root.Children.Add(overlayButton);
            this.Content = root;

            this.Closed += this.OnClosed;
        }

        private void OnVtkInitialized(object? sender, VtkRenderControlInitializedEventArgs e)
        {
            this._cone = vtkConeSource.New();
            this._cone.SetHeight(3.0);
            this._cone.SetRadius(1.0);
            this._cone.SetResolution(48);

            this._mapper = vtkPolyDataMapper.New();
            this._mapper.SetInputConnection(this._cone.GetOutputPort());

            this._actor = vtkActor.New();
            this._actor.SetMapper(this._mapper);
            this._actor.GetProperty().SetColor(0.95, 0.58, 0.22);

            e.Renderer.SetBackground(0.08, 0.1, 0.13);
            e.Renderer.AddActor(this._actor);
            e.Renderer.ResetCamera();

            if (sender is VtkOpenGlD3DImageRenderControl control)
            {
                control.Render();
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            this._actor?.Dispose();
            this._mapper?.Dispose();
            this._cone?.Dispose();
        }
    }
}
