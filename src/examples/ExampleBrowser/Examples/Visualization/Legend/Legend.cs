using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Legend", "Visualization",
    Description = "Shows named object symbols with distinct colors in a box legend.",
    SourceFiles = new[] { "Examples/Visualization/Legend/Legend.cs" })]
internal sealed class Legend : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Visualization/Legend/
        using var sphere = vtkSphereSource.New();
        sphere.SetRadius(1);
        sphere.SetThetaResolution(32);
        sphere.SetPhiResolution(24);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var legendBox = vtkCubeSource.New();
        legendBox.Update();
        using var legendSphere = vtkSphereSource.New();
        legendSphere.SetThetaResolution(16);
        legendSphere.SetPhiResolution(12);
        legendSphere.Update();
        using var legend = vtkLegendBoxActor.New();
        legend.SetNumberOfEntries(2);
        legend.SetEntry(0, legendBox.GetOutput(), "Box", new double[] { 1, 0.39, 0.28 });
        legend.SetEntry(1, legendSphere.GetOutput(), "Ball", new double[] { 1, 0.89, 0.21 });
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.AddActor(legend);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("Legend");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "Legend", screenshotPath);
    }
}
