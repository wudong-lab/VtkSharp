using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("RubberBandZoom", "Interaction",
    Description = "Zooms the camera to a rectangle selected with the rubber band.",
    SourceFiles = new[] { "Examples/Interaction/RubberBandZoom/RubberBandZoom.cs" })]
internal sealed class RubberBandZoom : ISmokeExample
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
        window.SetWindowName("RubberBandZoom");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleRubberBandZoom.New();
        interactor.SetInteractorStyle(style);
        renderer.GetActiveCamera().Zoom(0.5);

        ExampleRenderSupport.Finish(window, interactor, "RubberBandZoom", screenshotPath);
    }
}
