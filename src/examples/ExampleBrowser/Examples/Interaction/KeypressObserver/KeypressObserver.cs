using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("KeypressObserver", "Interaction",
    Description = "Reports the pressed key using a managed observer.",
    SourceFiles = new[] { "Examples/Interaction/KeypressObserver/KeypressObserver.cs" })]
internal sealed class KeypressObserver : ISmokeExample
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
        window.SetWindowName("KeypressObserver");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddKeyPressEventObserver(
            e => Debug.WriteLine($"Pressed: {e.KeySym}"), priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "KeypressObserver", screenshotPath);
    }
}
