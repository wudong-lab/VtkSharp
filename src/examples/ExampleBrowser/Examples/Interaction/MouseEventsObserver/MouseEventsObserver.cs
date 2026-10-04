using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("MouseEventsObserver", "Interaction",
    Description = "Observes mouse buttons and key presses while retaining the default camera interaction.",
    SourceFiles = new[] { "Examples/Interaction/MouseEventsObserver/MouseEventsObserver.cs" })]
internal sealed class MouseEventsObserver : ISmokeExample
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
        window.SetWindowName("MouseEventsObserver");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        using var leftObserver = interactor.AddLeftButtonPressEventObserver(
            _ => Debug.WriteLine("MouseEventsObserver: left button pressed."), priority: -1);
        using var rightObserver = interactor.AddRightButtonPressEventObserver(
            _ => Debug.WriteLine("MouseEventsObserver: right button pressed."), priority: -1);
        using var keyObserver = interactor.AddKeyPressEventObserver(
            e => Debug.WriteLine($"MouseEventsObserver: key {e.KeySym} pressed."), priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "MouseEventsObserver", screenshotPath);
    }
}
