using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("UserEvent", "Interaction",
    Description = "Invokes and observes a custom VTK user event when the user presses U.",
    SourceFiles = new[] { "Examples/Interaction/UserEvent/UserEvent.cs" })]
internal sealed class UserEvent : ISmokeExample
{
    private const uint CustomEvent = vtkCommand.UserEvent + 1;

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
        window.SetWindowName("UserEvent");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddObserver(CustomEvent, (_, _, _, _) => Debug.WriteLine("Custom event observed."));
        using var trigger = interactor.AddKeyPressEventObserver(e =>
        {
            if (e.KeySym.Equals("u", StringComparison.OrdinalIgnoreCase))
            {
                interactor.InvokeEvent(CustomEvent);
            }
        }, priority: 1);

        ExampleRenderSupport.Finish(window, interactor, "UserEvent", screenshotPath);
    }
}
