using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("WorldPointPicker", "Interaction",
    Description = "Reports the world-space point under the mouse using the depth buffer.",
    SourceFiles = new[] { "Examples/Interaction/WorldPointPicker/WorldPointPicker.cs" })]
internal sealed class WorldPointPicker : ISmokeExample
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
        window.SetWindowName("WorldPointPicker");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkWorldPointPicker.New();
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            picker.Pick(e.X, e.Y, 0, renderer);
            Span<double> position = stackalloc double[3];
            picker.GetPickPosition(position);
            Debug.WriteLine($"Picked world position: {position[0]}, {position[1]}, {position[2]}");
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "WorldPointPicker", screenshotPath);
    }
}
