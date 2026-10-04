using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("KeypressEvents", "Interaction",
    Description = "Handles selected keys with a managed observer while keeping trackball camera shortcuts.",
    SourceFiles = new[] { "Examples/Interaction/KeypressEvents/KeypressEvents.cs" })]
internal sealed class KeypressEvents : ISmokeExample
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
        window.SetWindowName("KeypressEvents");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddKeyPressEventObserver(e =>
        {
            Debug.WriteLine($"Pressed {e.KeySym}");
            if (e.KeySym == "Up") Debug.WriteLine("The up arrow was pressed.");
            if (e.KeySym == "a") Debug.WriteLine("The a key was pressed.");
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "KeypressEvents", screenshotPath);
    }
}
