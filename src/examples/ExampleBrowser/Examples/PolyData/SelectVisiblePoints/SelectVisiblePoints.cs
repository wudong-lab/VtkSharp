using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("SelectVisiblePoints", "PolyData",
    Description = "Uses the renderer depth buffer to count visible sphere points.",
    SourceFiles = new[] { "Examples/PolyData/SelectVisiblePoints/SelectVisiblePoints.cs" })]
internal sealed class SelectVisiblePoints : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(32);
        sphere.SetPhiResolution(24);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetRepresentationToPoints();
        actor.GetProperty().SetPointSize(5);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("SelectVisiblePoints");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var visiblePoints = vtkSelectVisiblePoints.New();
        visiblePoints.SetInputConnection(sphere.GetOutputPort());
        visiblePoints.SetRenderer(renderer);
        using var observer = interactor.AddKeyPressEventObserver(e =>
        {
            if (e.KeySym.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                visiblePoints.Update();
                Debug.WriteLine($"Visible points: {visiblePoints.GetOutput().GetNumberOfPoints()}");
            }
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "SelectVisiblePoints", screenshotPath);
    }
}
