using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("InterpolateMeshOnGrid", "PolyData",
    Description = "Resamples scattered terrain elevations onto a regular grid and triangulates both surfaces.",
    SourceFiles = new[] { "Examples/PolyData/InterpolateMeshOnGrid/InterpolateMeshOnGrid.cs" })]
internal sealed class InterpolateMeshOnGrid : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/InterpolateMeshOnGrid/
        const int gridSize = 10;
        using var randomPoints = vtkPoints.New();
        using var elevations = vtkFloatArray.New();
        elevations.SetName("ZValues");

        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(8775070);
        for (int i = 0; i < 100; i++)
        {
            var x = random.GetRangeValue(0, gridSize);
            random.Next();
            var y = random.GetRangeValue(0, gridSize);
            random.Next();
            var z = random.GetRangeValue(0, 1);
            random.Next();
            randomPoints.InsertNextPoint(x, y, 0);
            elevations.InsertNextTuple1(z);
        }

        using var randomPolyData = vtkPolyData.New();
        randomPolyData.SetPoints(randomPoints);
        randomPolyData.GetPointData().SetScalars(elevations);

        using var randomDelaunay = vtkDelaunay2D.New();
        randomDelaunay.SetInputData(randomPolyData);
        randomDelaunay.Update();

        using var gridPoints = vtkPoints.New();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
                gridPoints.InsertNextPoint(x, y, 0);
        }

        using var gridPolyData = vtkPolyData.New();
        gridPolyData.SetPoints(gridPoints);
        using var probe = vtkProbeFilter.New();
        probe.SetSourceData(randomDelaunay.GetOutput());
        probe.SetInputData(gridPolyData);
        probe.Update();

        using var gridWarp = vtkWarpScalar.New();
        gridWarp.SetInputConnection(probe.GetOutputPort());
        gridWarp.Update();
        using var randomWarp = vtkWarpScalar.New();
        randomWarp.SetInputConnection(randomDelaunay.GetOutputPort());
        randomWarp.Update();

        using var gridDelaunay = vtkDelaunay2D.New();
        gridDelaunay.SetInputConnection(gridWarp.GetOutputPort());

        using var colors = vtkNamedColors.New();
        using var randomMapper = vtkDataSetMapper.New();
        randomMapper.SetInputConnection(randomWarp.GetOutputPort());
        randomMapper.ScalarVisibilityOff();
        using var randomActor = vtkActor.New();
        randomActor.SetMapper(randomMapper);
        var salmon = colors.GetColor3d("Salmon");
        randomActor.GetProperty().SetColor(salmon.R, salmon.G, salmon.B);
        randomActor.GetProperty().SetPointSize(4);

        using var gridMapper = vtkDataSetMapper.New();
        gridMapper.SetInputConnection(gridDelaunay.GetOutputPort());
        gridMapper.ScalarVisibilityOff();
        using var gridActor = vtkActor.New();
        gridActor.SetMapper(gridMapper);
        var steelBlue = colors.GetColor3d("SteelBlue");
        gridActor.GetProperty().SetColor(steelBlue.R, steelBlue.G, steelBlue.B);
        gridActor.GetProperty().SetPointSize(4);

        using var renderer = vtkRenderer.New();
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.AddActor(randomActor);
        renderer.AddActor(gridActor);
        renderer.ResetCamera();

        using var window = vtkRenderWindow.New();
        window.SetWindowName("InterpolateMeshOnGrid");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        interactor.SetRenderWindow(window);

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
