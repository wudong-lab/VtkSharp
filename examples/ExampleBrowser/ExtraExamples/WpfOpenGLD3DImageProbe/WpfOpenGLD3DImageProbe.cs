using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VtkSharp.Wpf;

namespace VtkSharp.ExampleBrowser.ExtraExamples;

[Example("WPF OpenGL D3DImage Probe", "WPF",
    Description = "Validates OpenGL rendering into a D3D9Ex texture displayed by WPF D3DImage.",
    SourceFiles = new[] { "ExtraExamples/WpfOpenGLD3DImageProbe/WpfOpenGLD3DImageProbe.cs" })]
internal sealed class WpfOpenGLD3DImageProbe : IExample
{
    public void Run()
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF application dispatcher is not available.");

        dispatcher.Invoke(() =>
        {
            var window = new WpfOpenGLD3DImageProbeWindow
            {
                Owner = Application.Current.MainWindow
            };
            window.Show();
        });
    }

    private sealed class WpfOpenGLD3DImageProbeWindow : Window
    {
        public WpfOpenGLD3DImageProbeWindow()
        {
            this.Title = "VtkSharp WPF OpenGL D3DImage Probe";
            this.Width = 1000;
            this.Height = 720;
            this.MinWidth = 480;
            this.MinHeight = 360;

            var probe = new VtkOpenGLD3DImageProbeControl();

            var overlayButton = new Button
            {
                Content = "OpenGL/D3D9Ex",
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
            root.Children.Add(probe);
            root.Children.Add(overlayButton);
            this.Content = root;
        }
    }
}
