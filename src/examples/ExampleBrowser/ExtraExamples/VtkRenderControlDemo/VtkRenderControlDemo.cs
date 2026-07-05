using System.Windows;

namespace VtkSharp.ExampleBrowser.ExtraExamples.VtkRenderControlDemo;

[Example("VtkRenderControl Demo", "WPF",
    Description = "Recommended WPF VTK viewport using OpenGL/D3D9Ex interop and D3DImage.",
    SourceFiles = new[] { "ExtraExamples/VtkRenderControlDemo/VtkRenderControlDemo.cs" })]
internal sealed class VtkRenderControlDemo : IExample
{
    public void Run()
    {
        var dispatcher = Application.Current?.Dispatcher
                         ?? throw new InvalidOperationException("WPF application dispatcher is not available.");

        dispatcher.Invoke(() =>
        {
            var window = new VtkRenderControlDemoWindow()
            {
                Owner = Application.Current.MainWindow,
            };
            window.Show();
        });
    }
}