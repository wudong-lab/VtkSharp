using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("LegendScaleActor", "Visualization",
    Description = "Shows a spatial scale and axis ticks around a 3D surface.",
    SourceFiles = new[] { "Examples/Visualization/LegendScaleActor/LegendScaleActor.cs" })]
internal sealed class LegendScaleActor : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Annotation/LegendScaleActor/
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(32);
        sphere.SetPhiResolution(24);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var scale = vtkLegendScaleActor.New();
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.AddActor(scale);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("LegendScaleActor");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "LegendScaleActor", screenshotPath);
    }
}
