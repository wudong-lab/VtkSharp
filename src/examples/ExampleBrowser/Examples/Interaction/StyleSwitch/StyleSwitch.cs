using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("StyleSwitch", "Interaction",
    Description = "Uses VTK's built-in keyboard shortcuts to switch among actor/camera and joystick/trackball styles.",
    SourceFiles = new[] { "Examples/Interaction/StyleSwitch/StyleSwitch.cs" })]
internal sealed class StyleSwitch : ISmokeExample
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
        window.SetWindowName("StyleSwitch");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleSwitch.New();
        interactor.SetInteractorStyle(style);

        ExampleRenderSupport.Finish(window, interactor, "StyleSwitch", screenshotPath);
    }
}
