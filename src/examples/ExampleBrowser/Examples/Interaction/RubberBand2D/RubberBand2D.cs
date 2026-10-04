using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("RubberBand2D", "Interaction",
    Description = "Uses the 2D rubber band style and reports the mouse drag endpoints.",
    SourceFiles = new[] { "Examples/Interaction/RubberBand2D/RubberBand2D.cs" })]
internal sealed class RubberBand2D : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("RubberBand2D");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleRubberBand2D.New();
        interactor.SetInteractorStyle(style);
        var start = (X: 0, Y: 0);
        using var press = interactor.AddLeftButtonPressEventObserver(e => start = (e.X, e.Y));
        using var release = interactor.AddLeftButtonReleaseEventObserver(e =>
            Debug.WriteLine($"Drag: ({start.X}, {start.Y}) - ({e.X}, {e.Y})"));

        ExampleRenderSupport.Finish(window, interactor, "RubberBand2D", screenshotPath);
    }
}
