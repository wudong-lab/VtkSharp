using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("InteractorStyleUser", "Interaction",
    Description = "Demonstrates a custom interactor style with no default actions and a managed click callback.",
    SourceFiles = new[] { "Examples/Interaction/InteractorStyleUser/InteractorStyleUser.cs" })]
internal sealed class InteractorStyleUser : ISmokeExample
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
        window.SetWindowName("InteractorStyleUser");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleUser.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddLeftButtonPressEventObserver(
            _ => Debug.WriteLine("Click callback."));

        ExampleRenderSupport.Finish(window, interactor, "InteractorStyleUser", screenshotPath);
    }
}
