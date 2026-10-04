using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("SimpleElevationFilter", "Meshes",
    Description = "Colors a triangulated grid by projecting point coordinates onto a vector.",
    SourceFiles = new[] { "Examples/Meshes/SimpleElevationFilter/SimpleElevationFilter.cs" })]
internal sealed class SimpleElevationFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Meshes/SimpleElevationFilter/
        using var points = vtkPoints.New();
        const uint gridSize = 10;
        for (uint x = 0; x < gridSize; x++)
        for (uint y = 0; y < gridSize; y++)
            points.InsertNextPoint(x, y, (x + y) / (y + 1));

        using var input = vtkPolyData.New();
        input.SetPoints(points);
        using var triangulation = vtkDelaunay2D.New();
        triangulation.SetInputData(input);
        using var elevation = vtkSimpleElevationFilter.New();
        elevation.SetInputConnection(triangulation.GetOutputPort());
        elevation.SetVector(0, 0, 1);
        using var lut = vtkLookupTable.New();
        lut.SetTableRange(0, 9);
        lut.Build();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 9);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("SimpleElevationFilter");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "SimpleElevationFilter", screenshotPath);
    }
}
