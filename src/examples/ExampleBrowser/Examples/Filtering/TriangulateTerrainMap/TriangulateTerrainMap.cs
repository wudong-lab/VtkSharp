using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TriangulateTerrainMap", "Filtering",
    Description = "Creates a triangulated terrain surface from a regular XY grid with randomized elevations.",
    SourceFiles = new[] { "Examples/Filtering/TriangulateTerrainMap/TriangulateTerrainMap.cs" })]
internal sealed class TriangulateTerrainMap : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Filtering/TriangulateTerrainMap/
        using var points = vtkPoints.New();
        using var random = vtkMinimalStandardRandomSequence.New();
        random.Initialize(0);

        const int gridSize = 10;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                var z = random.GetValue() * 3.0;
                random.Next();
                points.InsertNextPoint(x, y, z);
            }
        }

        using var input = vtkPolyData.New();
        input.SetPoints(points);

        using var glyph = vtkVertexGlyphFilter.New();
        glyph.SetInputData(input);

        using var pointMapper = vtkPolyDataMapper.New();
        pointMapper.SetInputConnection(glyph.GetOutputPort());
        using var pointActor = vtkActor.New();
        pointActor.SetMapper(pointMapper);
        pointActor.GetProperty().SetRepresentationToPoints();
        pointActor.GetProperty().SetPointSize(3);
        pointActor.GetProperty().SetColor(1, 0, 0);

        using var delaunay = vtkDelaunay2D.New();
        delaunay.SetInputData(input);
        using var terrainMapper = vtkPolyDataMapper.New();
        terrainMapper.SetInputConnection(delaunay.GetOutputPort());
        using var terrainActor = vtkActor.New();
        terrainActor.SetMapper(terrainMapper);

        using var renderer = vtkRenderer.New();
        renderer.SetBackground(0.0, 0.5, 0.0);
        renderer.AddActor(terrainActor);
        renderer.AddActor(pointActor);

        using var window = vtkRenderWindow.New();
        window.SetWindowName("TriangulateTerrainMap");
        window.SetSize(640, 480);
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
