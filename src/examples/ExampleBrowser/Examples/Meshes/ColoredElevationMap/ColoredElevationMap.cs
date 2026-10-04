using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColoredElevationMap", "Meshes",
    Description = "Triangulates randomized terrain samples and colors vertices by elevation.",
    SourceFiles = new[] { "Examples/Meshes/ColoredElevationMap/ColoredElevationMap.cs" })]
internal sealed class ColoredElevationMap : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Meshes/ColoredElevationMap/
        using var points = vtkPoints.New();
        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(8775586);

        const int gridSize = 20;
        var elevations = new List<double>(gridSize * gridSize);
        var minElevation = double.MaxValue;
        var maxElevation = double.MinValue;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                random.Next();
                var px = x + random.GetRangeValue(-0.2, 0.2);
                random.Next();
                var py = y + random.GetRangeValue(-0.2, 0.2);
                random.Next();
                var pz = random.GetRangeValue(-0.5, 0.5);
                points.InsertNextPoint(px, py, pz);
                elevations.Add(pz);
                minElevation = Math.Min(minElevation, pz);
                maxElevation = Math.Max(maxElevation, pz);
            }
        }

        using var input = vtkPolyData.New();
        input.SetPoints(points);

        using var lookupTable = vtkLookupTable.New();
        lookupTable.SetTableRange(minElevation, maxElevation);
        lookupTable.Build();

        using var colors = vtkUnsignedCharArray.New();
        colors.SetNumberOfComponents(3);
        colors.SetName("Colors");
        Span<double> point = stackalloc double[3];
        Span<double> rgb = stackalloc double[3];
        foreach (var elevation in elevations)
        {
            lookupTable.GetColor(elevation, rgb);
            colors.InsertNextTuple3(
                (byte)(255 * rgb[0]),
                (byte)(255 * rgb[1]),
                (byte)(255 * rgb[2]));
        }

        input.GetPointData().SetScalars(colors);
        using var delaunay = vtkDelaunay2D.New();
        delaunay.SetInputData(input);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(delaunay.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        renderer.SetBackground(0.18, 0.22, 0.24);
        renderer.AddActor(actor);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ColoredElevationMap");
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
