using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("PointPicker", "Interaction",
    Description = "Reports the closest data point to a mouse click using vtkPointPicker.",
    SourceFiles = new[] { "Examples/Interaction/PointPicker/PointPicker.cs" })]
internal sealed class PointPicker : ISmokeExample
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
        window.SetWindowName("PointPicker");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkPointPicker.New();
        picker.SetTolerance(0.01);
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            picker.Pick(e.X, e.Y, 0, renderer);
            if (picker.GetPointId() < 0)
            {
                Debug.WriteLine("No data point picked.");
                return;
            }
            Span<double> position = stackalloc double[3];
            picker.GetPickPosition(position);
            Debug.WriteLine($"Picked point {picker.GetPointId()} at {position[0]}, {position[1]}, {position[2]}");
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "PointPicker", screenshotPath);
    }
}
