using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        private vtkAxesActor? _orientationAxes;
        private vtkOrientationMarkerWidget? _orientationWidget;
        private vtkPropPicker? _picker;
        private Button? _statusButton;
        private VtkOpenGlD3DImageRenderControl? _viewport;
        private VtkObserverHandle? _timerObserver;
        private int _animationTimerId;
        private Point? _leftButtonDownPosition;
        private bool _isPicked;

        public WpfOpenGLD3DImageViewportWindow()
        {
            this.Title = "VtkSharp WPF OpenGL D3DImage Viewport";
            this.Width = 1000;
            this.Height = 720;
            this.MinWidth = 480;
            this.MinHeight = 360;

            this._viewport = new VtkOpenGlD3DImageRenderControl();
            this._viewport.VtkRenderInitialized += this.OnVtkRenderInitialized;
            this._viewport.AddHandler(
                UIElement.MouseLeftButtonDownEvent,
                new MouseButtonEventHandler(this.OnViewportMouseLeftButtonDown),
                handledEventsToo: true);
            this._viewport.AddHandler(
                UIElement.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(this.OnViewportMouseLeftButtonUp),
                handledEventsToo: true);

            this._statusButton = new Button
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
            root.Children.Add(this._viewport);
            root.Children.Add(this._statusButton);
            this.Content = root;

            this.Closed += this.OnClosed;
        }

        private void OnVtkRenderInitialized(object? sender, VtkRenderInitializedEventArgs e)
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
            this._picker = vtkPropPicker.New();

            e.Renderer.SetBackground(0.08, 0.1, 0.13);
            e.Renderer.AddActor(this._actor);
            e.Renderer.ResetCamera();

            if (e.RenderWindowInteractor is not null)
            {
                this._orientationAxes = vtkAxesActor.New();

                this._orientationWidget = vtkOrientationMarkerWidget.New();
                this._orientationWidget.SetOrientationMarker(this._orientationAxes);
                this._orientationWidget.SetInteractor(e.RenderWindowInteractor);
                this._orientationWidget.SetViewport(0.0, 0.0, 0.22, 0.22);
                this._orientationWidget.EnabledOn();
                this._orientationWidget.InteractiveOn();

                this._timerObserver = e.RenderWindowInteractor.AddTimerEventObserver(this.OnTimer);
                this._animationTimerId = e.RenderWindowInteractor.CreateRepeatingTimer(33);
            }

            if (sender is VtkOpenGlD3DImageRenderControl control)
            {
                control.Render();
            }
        }

        private void OnViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this._viewport is null) return;

            this._leftButtonDownPosition = e.GetPosition(this._viewport);
        }

        private void OnViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this._viewport?.Renderer is null || this._picker is null || this._actor is null) return;

            var position = e.GetPosition(this._viewport);
            if (!this.IsClick(position))
            {
                this._leftButtonDownPosition = null;
                return;
            }

            this._leftButtonDownPosition = null;

            var pixelPosition = this.GetVtkDisplayPosition(position);
            var picked = this._picker.Pick(pixelPosition.X, pixelPosition.Y, 0.0, this._viewport.Renderer) != 0;
            var pickedActor = picked ? this._picker.GetActor() : null;

            this._isPicked = pickedActor?.NativePointer == this._actor.NativePointer && !this._isPicked;
            this._actor.GetProperty().SetColor(this._isPicked ? 0.28 : 0.95, this._isPicked ? 0.72 : 0.58, this._isPicked ? 1.0 : 0.22);

            if (this._statusButton is not null)
            {
                this._statusButton.Content = this._isPicked ? "Cone picked" : "OpenGL/D3D VTK";
            }

            this._viewport.RequestRender();
        }

        private void OnTimer(VtkTimerEventArgs e)
        {
            if (e.TimerId != this._animationTimerId || this._actor is null || this._viewport is null) return;

            this._actor.RotateY(0.5);
            this._viewport.RequestRender();
        }

        private bool IsClick(Point position)
        {
            if (this._leftButtonDownPosition is not { } start) return false;

            var delta = position - start;
            return delta.Length <= 4.0;
        }

        private PixelPoint GetVtkDisplayPosition(Point position)
        {
            if (this._viewport is null) return new PixelPoint(0, 0);

            var source = PresentationSource.FromVisual(this._viewport);
            var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            var pixelSize = this.GetPixelSize();
            var x = (int)Math.Round(position.X * transform.M11);
            var y = pixelSize.Height - 1 - (int)Math.Round(position.Y * transform.M22);
            return new PixelPoint(x, y);
        }

        private PixelSize GetPixelSize()
        {
            if (this._viewport is null) return new PixelSize(1, 1);

            var source = PresentationSource.FromVisual(this._viewport);
            var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
            var width = Math.Max(1, (int)Math.Ceiling(this._viewport.ActualWidth * transform.M11));
            var height = Math.Max(1, (int)Math.Ceiling(this._viewport.ActualHeight * transform.M22));
            return new PixelSize(width, height);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            this._orientationWidget?.EnabledOff();
            if (this._animationTimerId != 0)
            {
                this._viewport?.RenderWindowInteractor?.DestroyTimer(this._animationTimerId);
                this._animationTimerId = 0;
            }

            this._timerObserver?.Dispose();
            this._orientationWidget?.Dispose();
            this._orientationAxes?.Dispose();
            this._picker?.Dispose();
            this._actor?.Dispose();
            this._mapper?.Dispose();
            this._cone?.Dispose();
        }

        private readonly record struct PixelPoint(int X, int Y);
        private readonly record struct PixelSize(int Width, int Height);
    }
}
