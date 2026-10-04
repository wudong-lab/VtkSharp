using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TrackballCamera", "Interaction",
    Description = "Demonstrates camera rotation, pan, and zoom using the trackball camera style.",
    SourceFiles = new[] { "Examples/Interaction/TrackballCamera/TrackballCamera.cs" })]
internal sealed class TrackballCamera : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        sphere.SetCenter(1, 0, 0);
        using var sphereMapper = vtkPolyDataMapper.New();
        sphereMapper.SetInputConnection(sphere.GetOutputPort());
        using var sphereActor = vtkActor.New();
        sphereActor.SetMapper(sphereMapper);
        sphereActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var cone = vtkConeSource.New();
        using var coneMapper = vtkPolyDataMapper.New();
        coneMapper.SetInputConnection(cone.GetOutputPort());
        using var coneActor = vtkActor.New();
        coneActor.SetMapper(coneMapper);
        coneActor.GetProperty().SetColor(colors.GetColor3d("LightGoldenrodYellow"));

        using var renderer = vtkRenderer.New();
        renderer.AddActor(sphereActor);
        renderer.AddActor(coneActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("TrackballCamera");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        ExampleRenderSupport.Finish(window, interactor, "TrackballCamera", screenshotPath);
    }
}
