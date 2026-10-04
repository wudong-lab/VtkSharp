using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("DoubleClick", "Interaction",
    Description = "Detects two nearby left clicks while preserving trackball camera interaction.",
    SourceFiles = new[] { "Examples/Interaction/DoubleClick/DoubleClick.cs" })]
internal sealed class DoubleClick : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        sphere.SetRadius(5.0);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("DoubleClick");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        var clicks = 0;
        var previousX = 0;
        var previousY = 0;
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            clicks++;
            var dx = e.X - previousX;
            var dy = e.Y - previousY;
            previousX = e.X;
            previousY = e.Y;
            if (Math.Sqrt((double)dx * dx + (double)dy * dy) > 5) clicks = 1;
            if (clicks == 2)
            {
                Debug.WriteLine("Double clicked.");
                clicks = 0;
            }
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "DoubleClick", screenshotPath);
    }
}
