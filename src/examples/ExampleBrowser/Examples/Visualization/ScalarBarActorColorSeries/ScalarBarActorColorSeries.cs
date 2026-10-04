using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ScalarBarActorColorSeries", "Visualization",
    Description = "Colors an elevation sphere with a ColorBrewer series and matching scalar bar.",
    SourceFiles = new[] { "Examples/Visualization/ScalarBarActorColorSeries/ScalarBarActorColorSeries.cs" })]
internal sealed class ScalarBarActorColorSeries : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Visualization/ScalarBarActorColorSeries/
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(48);
        sphere.SetPhiResolution(32);
        using var elevation = vtkElevationFilter.New();
        elevation.SetInputConnection(sphere.GetOutputPort());
        elevation.SetLowPoint(0, 0, -1);
        elevation.SetHighPoint(0, 0, 1);
        elevation.SetScalarRange(0, 1);
        using var series = vtkColorSeries.New();
        series.SetColorSchemeByName("Brewer Diverging Brown-Blue-Green (8)");
        using var lut = vtkLookupTable.New();
        series.BuildLookupTable(lut, 0); // vtkColorSeries::ORDINAL
        lut.SetTableRange(0, 1);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 1);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var scalarBar = vtkScalarBarActor.New();
        scalarBar.SetLookupTable(lut);
        scalarBar.SetTitle("Elevation");
        scalarBar.SetNumberOfLabels(5);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.AddActor(scalarBar);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ScalarBarActorColorSeries");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ScalarBarActorColorSeries", screenshotPath);
    }
}
