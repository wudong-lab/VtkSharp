using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ElevationFilter", "Meshes",
    Description = "Generates an elevation scalar on a triangulated height map.",
    SourceFiles = new[] { "Examples/Meshes/ElevationFilter/ElevationFilter.cs" })]
internal sealed class ElevationFilter : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Meshes/ElevationFilter/
        using var points = vtkPoints.New();
        const int gridSize = 10;
        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                points.InsertNextPoint(x, y, (x + y) / (double)(y + 1));

        using var input = vtkPolyData.New();
        input.SetPoints(points);
        using var delaunay = vtkDelaunay2D.New();
        delaunay.SetInputData(input);
        using var elevation = vtkElevationFilter.New();
        elevation.SetInputConnection(delaunay.GetOutputPort());
        elevation.SetLowPoint(0, 0, 0);
        elevation.SetHighPoint(0, 0, 9);
        elevation.SetScalarRange(0, 9);

        using var lut = vtkLookupTable.New();
        lut.SetTableRange(0, 9);
        lut.Build();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 9);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("ForestGreen"));
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ElevationFilter");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Elevation(-35);
        window.Render();
        if (screenshotPath is not null)
        {
            using var image = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        interactor.Start();
    }
}
