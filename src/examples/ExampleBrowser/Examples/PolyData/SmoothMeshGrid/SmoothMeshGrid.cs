using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("SmoothMeshGrid", "PolyData",
    Description = "Compares an irregular height grid with Loop and Butterfly subdivision surfaces.",
    SourceFiles = new[] { "Examples/PolyData/SmoothMeshGrid/SmoothMeshGrid.cs" })]
internal sealed class SmoothMeshGrid : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Python/PolyData/SmoothMeshGrid/
        const int size = 32;
        var heights = new double[size, size];
        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(1);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                heights[i, j] = random.GetRangeValue(0, 5);
                random.Next();
            }
        }

        using var points = vtkPoints.New();
        using var triangles = vtkCellArray.New();
        using var pointColors = vtkUnsignedCharArray.New();
        pointColors.SetNumberOfComponents(3);
        using var firstTriangle = vtkTriangle.New();
        using var secondTriangle = vtkTriangle.New();
        for (int i = 0; i < size - 1; i++)
        {
            for (int j = 0; j < size - 1; j++)
            {
                var first = (long)points.GetNumberOfPoints();
                points.InsertNextPoint(i, j, heights[i, j]);
                points.InsertNextPoint(i, j + 1, heights[i, j + 1]);
                points.InsertNextPoint(i + 1, j, heights[i + 1, j]);
                firstTriangle.GetPointIds().SetId(0, first);
                firstTriangle.GetPointIds().SetId(1, first + 1);
                firstTriangle.GetPointIds().SetId(2, first + 2);
                triangles.InsertNextCell(firstTriangle);

                var second = (long)points.GetNumberOfPoints();
                points.InsertNextPoint(i, j + 1, heights[i, j + 1]);
                points.InsertNextPoint(i + 1, j + 1, heights[i + 1, j + 1]);
                points.InsertNextPoint(i + 1, j, heights[i + 1, j]);
                secondTriangle.GetPointIds().SetId(0, second);
                secondTriangle.GetPointIds().SetId(1, second + 1);
                secondTriangle.GetPointIds().SetId(2, second + 2);
                triangles.InsertNextCell(secondTriangle);

                var red = (byte)(i / (float)size * 255);
                var green = (byte)(j / (float)size * 255);
                for (int k = 0; k < 6; k++)
                    pointColors.InsertNextTuple3(red, green, 0);
            }
        }

        using var terrain = vtkPolyData.New();
        terrain.SetPoints(points);
        terrain.GetPointData().SetScalars(pointColors);
        terrain.SetPolys(triangles);

        using var clean = vtkCleanPolyData.New();
        clean.SetInputData(terrain);
        using var loop = vtkLoopSubdivisionFilter.New();
        loop.SetNumberOfSubdivisions(3);
        loop.SetInputConnection(clean.GetOutputPort());
        using var butterfly = vtkButterflySubdivisionFilter.New();
        butterfly.SetNumberOfSubdivisions(3);
        butterfly.SetInputConnection(clean.GetOutputPort());

        using var originalMapper = vtkPolyDataMapper.New();
        originalMapper.SetInputData(terrain);
        using var originalActor = vtkActor.New();
        originalActor.SetMapper(originalMapper);

        using var loopMapper = vtkPolyDataMapper.New();
        loopMapper.SetInputConnection(loop.GetOutputPort());
        using var loopActor = vtkActor.New();
        loopActor.SetMapper(loopMapper);
        loopActor.SetPosition(size, 0, 0);

        using var butterflyMapper = vtkPolyDataMapper.New();
        butterflyMapper.SetInputConnection(butterfly.GetOutputPort());
        using var butterflyActor = vtkActor.New();
        butterflyActor.SetMapper(butterflyMapper);
        butterflyActor.SetPosition(size * 2, 0, 0);

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        renderer.AddActor(originalActor);
        renderer.AddActor(loopActor);
        renderer.AddActor(butterflyActor);
        renderer.SetBackground(colors.GetColor3d("AliceBlue"));

        using var window = vtkRenderWindow.New();
        window.SetWindowName("SmoothMeshGrid");
        window.SetSize(900, 300);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        renderer.ResetCamera();
        renderer.GetActiveCamera().Elevation(-45);
        renderer.GetActiveCamera().Zoom(2.4);
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
