using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("LineOnMesh", "DataManipulation",
    Description = "Projects vertical samples onto a subdivided terrain and connects them with a spline.",
    SourceFiles = new[] { "Examples/DataManipulation/LineOnMesh/LineOnMesh.cs" })]
internal sealed class LineOnMesh : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Python/DataManipulation/LineOnMesh/
        const int size = 32;
        var heights = new double[size, size];
        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(3);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                heights[i, j] = random.GetRangeValue(0, 4);
                random.Next();
            }
        }

        using var points = vtkPoints.New();
        using var triangles = vtkCellArray.New();
        using var triangle = vtkTriangle.New();
        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - 1; j++)
            {
                var first = (long)points.GetNumberOfPoints();
                points.InsertNextPoint(i, j, heights[i, j]);
                points.InsertNextPoint(i, j + 1, heights[i, j + 1]);
                points.InsertNextPoint(i + 1, j, heights[i + 1, j]);
                triangle.GetPointIds().SetId(0, first);
                triangle.GetPointIds().SetId(1, first + 1);
                triangle.GetPointIds().SetId(2, first + 2);
                triangles.InsertNextCell(triangle);

                var second = (long)points.GetNumberOfPoints();
                points.InsertNextPoint(i, j + 1, heights[i, j + 1]);
                points.InsertNextPoint(i + 1, j + 1, heights[i + 1, j + 1]);
                points.InsertNextPoint(i + 1, j, heights[i + 1, j]);
                triangle.GetPointIds().SetId(0, second);
                triangle.GetPointIds().SetId(1, second + 1);
                triangle.GetPointIds().SetId(2, second + 2);
                triangles.InsertNextCell(triangle);
            }
        }

        using var terrain = vtkPolyData.New();
        terrain.SetPoints(points);
        terrain.SetPolys(triangles);
        using var clean = vtkCleanPolyData.New();
        clean.SetInputData(terrain);
        using var smooth = vtkLoopSubdivisionFilter.New();
        smooth.SetNumberOfSubdivisions(3);
        smooth.SetInputConnection(clean.GetOutputPort());
        smooth.Update();

        using var locator = vtkCellLocator.New();
        locator.SetDataSet(smooth.GetOutput());
        locator.BuildLocator();
        using var intersectionPoints = vtkPoints.New();
        using var hitCell = vtkGenericCell.New();
        const int sampleCount = 100;
        for (int i = 0; i < sampleCount; i++)
        {
            double x = 2.0 + i * (20.0 / sampleCount);
            using var hits = vtkPoints.New();
            using var hitCellIds = vtkIdList.New();
            locator.IntersectWithLine(new[] { x, 16.0, -1.0 }, new[] { x, 16.0, 6.0 }, 0.001, hits, hitCellIds, hitCell);
            if (hits.GetNumberOfPoints() == 0)
                continue;
            double[] hit = new double[3];
            hits.GetPoint(0, hit);
            intersectionPoints.InsertNextPoint(hit[0], hit[1], hit[2] + 0.01);
        }

        using var spline = vtkParametricSpline.New();
        spline.SetPoints(intersectionPoints);
        using var splineSource = vtkParametricFunctionSource.New();
        splineSource.SetUResolution(sampleCount);
        splineSource.SetParametricFunction(spline);

        using var terrainMapper = vtkPolyDataMapper.New();
        terrainMapper.SetInputConnection(smooth.GetOutputPort());
        using var terrainActor = vtkActor.New();
        terrainActor.SetMapper(terrainMapper);
        terrainActor.GetProperty().SetInterpolationToFlat();
        using var lineMapper = vtkPolyDataMapper.New();
        lineMapper.SetInputConnection(splineSource.GetOutputPort());
        using var lineActor = vtkActor.New();
        lineActor.SetMapper(lineMapper);
        using var colors = vtkNamedColors.New();
        lineActor.GetProperty().SetColor(colors.GetColor3d("Red"));
        lineActor.GetProperty().SetLineWidth(3);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(terrainActor);
        renderer.AddActor(lineActor);
        renderer.SetBackground(colors.GetColor3d("Cornsilk"));
        using var window = vtkRenderWindow.New();
        window.SetWindowName("LineOnMesh");
        window.SetSize(800, 800);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Elevation(-45);
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
