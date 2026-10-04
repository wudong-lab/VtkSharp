using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ShiftAndControl", "Interaction",
    Description = "Reports modifier keys when clicking an actor and supports actor trackball interaction.",
    SourceFiles = new[] { "Examples/Interaction/ShiftAndControl/ShiftAndControl.cs" })]
internal sealed class ShiftAndControl : ISmokeExample
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
        window.SetWindowName("ShiftAndControl");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballActor.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            if (e.ShiftKey) Debug.Write("Shift held. ");
            if (e.ControlKey) Debug.Write("Control held. ");
            if (e.AltKey) Debug.Write("Alt held. ");
            Debug.WriteLine("Pressed left mouse button.");
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "ShiftAndControl", screenshotPath);
    }
}
