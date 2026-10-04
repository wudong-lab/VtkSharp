using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ScalarBarWidget", "Visualization",
    Description = "Shows an interactive scalar bar over an elevation-colored sphere.",
    SourceFiles = new[] { "Examples/Visualization/ScalarBarWidget/ScalarBarWidget.cs" })]
internal sealed class ScalarBarWidget : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Widgets/ScalarBarWidget/
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(48);
        sphere.SetPhiResolution(32);
        using var elevation = vtkElevationFilter.New();
        elevation.SetInputConnection(sphere.GetOutputPort());
        elevation.SetLowPoint(0, 0, -1);
        elevation.SetHighPoint(0, 0, 1);
        elevation.SetScalarRange(0, 1);
        using var lut = vtkLookupTable.New();
        lut.SetTableRange(0, 1);
        lut.Build();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 1);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ScalarBarWidget");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var widget = vtkScalarBarWidget.New();
        widget.SetInteractor(interactor);
        using var scalarBar = widget.GetScalarBarActor();
        scalarBar.SetLookupTable(lut);
        scalarBar.SetTitle("Elevation");
        scalarBar.SetNumberOfLabels(5);
        widget.EnabledOn();
        ExampleRenderSupport.Finish(window, interactor, "ScalarBarWidget", screenshotPath);
    }
}
