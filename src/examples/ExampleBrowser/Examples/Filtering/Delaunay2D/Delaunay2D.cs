using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Delaunay2D", "Filtering",
    Description = "Builds a Delaunay surface from XY grid samples with varying elevations.",
    SourceFiles = new[] { "Examples/Filtering/Delaunay2D/Delaunay2D.cs" })]
internal sealed class Delaunay2D : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Filtering/Delaunay2D/
        using var points = vtkPoints.New();
        const int gridSize = 10;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
                points.InsertNextPoint(x, y, (x + y) / (double)(y + 1));
        }

        using var terrain = vtkPolyData.New();
        terrain.SetPoints(points);

        using var delaunay = vtkDelaunay2D.New();
        delaunay.SetInputData(terrain);

        using var terrainMapper = vtkPolyDataMapper.New();
        terrainMapper.SetInputConnection(delaunay.GetOutputPort());
        using var terrainActor = vtkActor.New();
        terrainActor.SetMapper(terrainMapper);
        terrainActor.GetProperty().SetColor(0.98, 0.94, 0.66);
        terrainActor.GetProperty().EdgeVisibilityOn();
        terrainActor.GetProperty().SetEdgeColor(0.39, 0.58, 0.93);
        terrainActor.GetProperty().SetLineWidth(2);

        using var glyph = vtkVertexGlyphFilter.New();
        glyph.SetInputData(terrain);
        using var pointMapper = vtkPolyDataMapper.New();
        pointMapper.SetInputConnection(glyph.GetOutputPort());
        using var pointActor = vtkActor.New();
        pointActor.SetMapper(pointMapper);
        pointActor.GetProperty().SetColor(1.0, 0.08, 0.58);
        pointActor.GetProperty().SetPointSize(8);
        pointActor.GetProperty().RenderPointsAsSpheresOn();

        using var renderer = vtkRenderer.New();
        renderer.SetBackground(0.69, 0.88, 0.90);
        renderer.AddActor(terrainActor);
        renderer.AddActor(pointActor);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("Delaunay2D");
        window.SetSize(600, 600);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        renderer.ResetCamera();
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
