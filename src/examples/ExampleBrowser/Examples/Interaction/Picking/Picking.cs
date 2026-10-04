using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Picking", "Interaction",
    Description = "Picks scene actors, reports the world position, and places a small marker at a hit.",
    SourceFiles = new[] { "Examples/Interaction/Picking/Picking.cs" })]
internal sealed class Picking : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var plane = vtkPlaneSource.New();
        using var planeMapper = vtkPolyDataMapper.New();
        planeMapper.SetInputConnection(plane.GetOutputPort());
        using var planeActor = vtkActor.New();
        planeActor.SetMapper(planeMapper);
        planeActor.GetProperty().SetColor(colors.GetColor3d("LightGoldenrodYellow"));

        using var renderer = vtkRenderer.New();
        renderer.AddActor(planeActor);
        renderer.SetBackground(colors.GetColor3d("DodgerBlue"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("Picking");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkPropPicker.New();
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            picker.Pick(e.X, e.Y, 0, renderer);
            var pickedActor = picker.GetActor();
            if (pickedActor.NativePointer == 0)
            {
                Debug.WriteLine("No actor picked.");
                return;
            }

            Span<double> position = stackalloc double[3];
            picker.GetPickPosition(position);
            Debug.WriteLine($"Pick position (world coordinates): {position[0]}, {position[1]}, {position[2]}");
            using var marker = vtkSphereSource.New();
            marker.SetCenter(position[0], position[1], position[2]);
            marker.SetRadius(0.1);
            using var markerMapper = vtkPolyDataMapper.New();
            markerMapper.SetInputConnection(marker.GetOutputPort());
            using var markerActor = vtkActor.New();
            markerActor.SetMapper(markerMapper);
            markerActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
            renderer.AddActor(markerActor);
            window.Render();
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "Picking", screenshotPath);
    }
}
