using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("MouseEvents", "Interaction",
    Description = "Reports mouse button events while preserving the default trackball camera behavior.",
    SourceFiles = new[] { "Examples/Interaction/MouseEvents/MouseEvents.cs" })]
internal sealed class MouseEvents : ISmokeExample
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
        window.SetWindowName("MouseEvents");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        using var leftObserver = interactor.AddLeftButtonPressEventObserver(
            _ => Debug.WriteLine("Pressed left mouse button."), priority: -1);
        using var middleObserver = interactor.AddObserver(vtkCommand.MiddleButtonPressEvent,
            (_, _) => Debug.WriteLine("Pressed middle mouse button."), priority: -1);
        using var rightObserver = interactor.AddRightButtonPressEventObserver(
            _ => Debug.WriteLine("Pressed right mouse button."), priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "MouseEvents", screenshotPath);
    }
}
