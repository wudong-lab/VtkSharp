using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BandedPolyDataContourFilter", "VisualizationAlgorithms",
    Description = "Divides a point-scalar plane into discrete cell bands.",
    SourceFiles = new[] { "Examples/VisualizationAlgorithms/BandedPolyDataContourFilter/BandedPolyDataContourFilter.cs" })]
internal sealed class BandedPolyDataContourFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/BandedPolyDataContourFilter/
        using var plane = vtkPlaneSource.New();
        plane.SetXResolution(12);
        plane.SetYResolution(8);
        plane.Update();
        using var values = vtkFloatArray.New();
        for (long y = 0; y <= 8; y++)
        for (long x = 0; x <= 12; x++)
            values.InsertNextTuple1(100.0 * x / 12);
        plane.GetOutput().GetPointData().SetScalars(values);
        using var bands = vtkBandedPolyDataContourFilter.New();
        bands.SetInputData(plane.GetOutput());
        bands.GenerateValues(5, 25, 75);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(bands.GetOutputPort());
        mapper.SetScalarModeToUseCellData();
        mapper.SetScalarRange(0, 4);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("BandedPolyDataContourFilter");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "BandedPolyDataContourFilter", screenshotPath);
    }
}
