using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("RubberBandPick", "Interaction",
    Description = "Starts rubber band selection by pressing R, then dragging with the left mouse button.",
    SourceFiles = new[] { "Examples/Interaction/RubberBandPick/RubberBandPick.cs" })]
internal sealed class RubberBandPick : ISmokeExample
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
        actor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("RubberBandPick");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleRubberBandPick.New();
        interactor.SetInteractorStyle(style);

        ExampleRenderSupport.Finish(window, interactor, "RubberBandPick", screenshotPath);
    }
}
