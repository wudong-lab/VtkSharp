using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("InterpolateTerrain", "PolyData",
    Description = "Compares raster probe interpolation with vertical ray intersections against a TIN.",
    SourceFiles = new[] { "Examples/PolyData/InterpolateTerrain/InterpolateTerrain.cs" })]
internal sealed class InterpolateTerrain : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/InterpolateTerrain/
        using var image = vtkImageData.New();
        image.SetDimensions(10, 10, 1);
        image.AllocateScalars(11, 1); // VTK_DOUBLE

        using var terrainPoints = vtkPoints.New();
        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(8775070);
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                var elevation = random.GetRangeValue(-1, 1);
                random.Next();
                terrainPoints.InsertNextPoint(x, y, elevation);
                image.SetScalarComponentFromDouble(x, y, 0, 0, elevation);
            }
        }

        using var terrain = vtkPolyData.New();
        terrain.SetPoints(terrainPoints);
        using var delaunay = vtkDelaunay2D.New();
        delaunay.SetInputData(terrain);
        delaunay.Update();

        using var probePoints = vtkPoints.New();
        probePoints.InsertNextPoint(5.2, 3.2, 0);
        probePoints.InsertNextPoint(5.0, 3.0, 0);
        probePoints.InsertNextPoint(0.0, 0.0, 0);
        using var probeInput = vtkPolyData.New();
        probeInput.SetPoints(probePoints);
        using var probe = vtkProbeFilter.New();
        probe.SetSourceData(image);
        probe.SetInputData(probeInput);
        probe.Update();

        using var probeOutput = probe.GetOutput();
        using var sampledValues = probeOutput.GetPointData().GetScalars();
        for (long i = 0; i < probePoints.GetNumberOfPoints(); i++)
            Debug.WriteLine($"Raster interpolation at probe {i}: {sampledValues.GetTuple1(i):F4}");

        using var locator = vtkCellLocator.New();
        locator.SetDataSet(delaunay.GetOutput());
        locator.BuildLocator();
        using var intersections = vtkPoints.New();
        using var cellIds = vtkIdList.New();
        using var genericCell = vtkGenericCell.New();
        Span<double> rayStart = stackalloc double[3];
        Span<double> rayEnd = stackalloc double[3];
        for (long i = 0; i < probePoints.GetNumberOfPoints(); i++)
        {
            probePoints.GetPoint(i, rayStart);
            probePoints.GetPoint(i, rayEnd);
            rayStart[2] += 1000;
            rayEnd[2] -= 1000;
            var hitCount = locator.IntersectWithLine(rayStart, rayEnd, 0.0001, intersections, cellIds, genericCell);
            Debug.WriteLine($"TIN ray intersections at probe {i}: {hitCount}");
        }

        using var terrainMapper = vtkPolyDataMapper.New();
        terrainMapper.SetInputConnection(delaunay.GetOutputPort());
        using var terrainActor = vtkActor.New();
        terrainActor.SetMapper(terrainMapper);
        terrainActor.GetProperty().SetColor(0.48, 0.62, 0.48);
        terrainActor.GetProperty().EdgeVisibilityOn();
        terrainActor.GetProperty().SetEdgeColor(0.2, 0.28, 0.2);

        using var hitData = vtkPolyData.New();
        hitData.SetPoints(intersections);
        using var hitGlyph = vtkVertexGlyphFilter.New();
        hitGlyph.SetInputData(hitData);
        using var hitMapper = vtkPolyDataMapper.New();
        hitMapper.SetInputConnection(hitGlyph.GetOutputPort());
        using var hitActor = vtkActor.New();
        hitActor.SetMapper(hitMapper);
        hitActor.GetProperty().SetColor(1, 0.1, 0.1);
        hitActor.GetProperty().SetPointSize(10);
        hitActor.GetProperty().RenderPointsAsSpheresOn();

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.AddActor(terrainActor);
        renderer.AddActor(hitActor);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("InterpolateTerrain");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        renderer.ResetCamera();
        window.Render();
        if (screenshotPath is not null)
        {
            using var result = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(result);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        interactor.Start();
    }
}
