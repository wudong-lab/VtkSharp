using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("PickableOff", "Interaction",
    Description = "Shows that an actor with PickableOff cannot be selected or dragged.",
    SourceFiles = new[] { "Examples/Interaction/PickableOff/PickableOff.cs" })]
internal sealed class PickableOff : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var cone0 = vtkConeSource.New();
        using var mapper0 = vtkPolyDataMapper.New();
        mapper0.SetInputConnection(cone0.GetOutputPort());
        using var actor0 = vtkActor.New();
        actor0.SetMapper(mapper0);
        actor0.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var cone1 = vtkConeSource.New();
        cone1.SetCenter(2, 0, 0);
        using var mapper1 = vtkPolyDataMapper.New();
        mapper1.SetInputConnection(cone1.GetOutputPort());
        using var actor1 = vtkActor.New();
        actor1.SetMapper(mapper1);
        actor1.PickableOff();
        actor1.GetProperty().SetColor(colors.GetColor3d("LightGoldenrodYellow"));

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor0);
        renderer.AddActor(actor1);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        renderer.GetActiveCamera().Zoom(0.9);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("PickableOff");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballActor.New();
        interactor.SetInteractorStyle(style);

        ExampleRenderSupport.Finish(window, interactor, "PickableOff", screenshotPath);
    }
}
