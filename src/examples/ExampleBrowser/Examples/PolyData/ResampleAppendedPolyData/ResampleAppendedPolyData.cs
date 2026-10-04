using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ResampleAppendedPolyData", "PolyData",
    Description = "Resamples a level terrain after appending randomly placed solids, using vertical line intersections.",
    SourceFiles = new[] { "Examples/PolyData/ResampleAppendedPolyData/ResampleAppendedPolyData.cs" })]
internal sealed class ResampleAppendedPolyData : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/ResampleAppendedPolyData/
        const double min = -10;
        const double max = 10;
        const int terrainResolution = 100;
        const int numberOfObjects = 200;
        const int probeResolution = 50;

        using var terrain = vtkPlaneSource.New();
        terrain.SetOrigin(min, min, 0);
        terrain.SetPoint2(min, max, 0);
        terrain.SetPoint1(max, min, 0);
        terrain.SetXResolution(terrainResolution);
        terrain.SetYResolution(terrainResolution);
        terrain.Update();

        using var append = vtkAppendPolyData.New();
        append.AddInputData(terrain.GetOutput());
        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(4355412);

        for (int i = 0; i < numberOfObjects; i++)
        {
            var solidType = (int)random.GetRangeValue(0, 5);
            random.Next();
            var scale = random.GetRangeValue(0.2, 1.5);
            random.Next();
            var x = random.GetRangeValue(min + 1, max - 1);
            random.Next();
            var y = random.GetRangeValue(min + 1, max - 1);
            random.Next();

            using var solid = vtkPlatonicSolidSource.New();
            switch (solidType)
            {
                case 0: solid.SetSolidTypeToCube(); break;
                case 1: solid.SetSolidTypeToTetrahedron(); break;
                case 2: solid.SetSolidTypeToOctahedron(); break;
                case 3: solid.SetSolidTypeToIcosahedron(); break;
                default: solid.SetSolidTypeToDodecahedron(); break;
            }

            using var transform = vtkTransform.New();
            transform.Translate(x, y, 0);
            transform.Scale(scale, scale, scale);
            using var transformed = vtkTransformFilter.New();
            transformed.SetTransform(transform);
            transformed.SetInputConnection(solid.GetOutputPort());
            transformed.Update();

            using var transformedData = transformed.GetOutputDataObject(0);
            using var polyData = vtkPolyData.New();
            polyData.DeepCopy(transformedData);
            append.AddInputData(polyData);
        }
        append.Update();

        using var locator = vtkCellLocator.New();
        locator.SetDataSet(append.GetOutput());
        locator.BuildLocator();

        using var probeTerrain = vtkPlaneSource.New();
        probeTerrain.SetOrigin(min, min, 0);
        probeTerrain.SetPoint1(max, min, 0);
        probeTerrain.SetPoint2(min, max, 0);
        probeTerrain.SetXResolution(probeResolution);
        probeTerrain.SetYResolution(probeResolution);
        probeTerrain.Update();

        using var probeOutput = probeTerrain.GetOutput();
        using var probePoints = probeOutput.GetPoints();
        using var intersections = vtkPoints.New();
        using var cellIds = vtkIdList.New();
        using var genericCell = vtkGenericCell.New();
        Span<double> rayStart = stackalloc double[3];
        Span<double> rayEnd = stackalloc double[3];
        Span<double> intersection = stackalloc double[3];
        Span<double> probePoint = stackalloc double[3];
        for (long i = 0; i < probePoints.GetNumberOfPoints(); i++)
        {
            probePoints.GetPoint(i, rayStart);
            probePoints.GetPoint(i, rayEnd);
            rayStart[2] += 100000;
            rayEnd[2] -= 100000;
            intersections.Initialize();
            var hitCount = locator.IntersectWithLine(rayStart, rayEnd, 0.0001, intersections, cellIds, genericCell);
            if (hitCount == 0)
                continue;

            probePoints.GetPoint(i, probePoint);
            intersections.GetPoint(0, intersection);
            probePoints.SetPoint(i, probePoint[0], probePoint[1], intersection[2]);
        }
        using var colors = vtkNamedColors.New();
        using var originalMapper = vtkPolyDataMapper.New();
        originalMapper.SetInputConnection(append.GetOutputPort());
        originalMapper.ScalarVisibilityOff();
        using var originalActor = vtkActor.New();
        originalActor.SetMapper(originalMapper);
        originalActor.GetProperty().SetColor(colors.GetColor3d("Wheat"));

        using var resampledMapper = vtkPolyDataMapper.New();
        resampledMapper.SetInputData(probeOutput);
        resampledMapper.ScalarVisibilityOff();
        using var resampledActor = vtkActor.New();
        resampledActor.SetMapper(resampledMapper);
        resampledActor.GetProperty().SetRepresentationToWireframe();
        resampledActor.GetProperty().SetColor(colors.GetColor3d("Seashell"));

        using var leftRenderer = vtkRenderer.New();
        leftRenderer.SetViewport(0, 0, 0.5, 1);
        leftRenderer.UseHiddenLineRemovalOn();
        leftRenderer.AddActor(originalActor);
        leftRenderer.SetBackground(colors.GetColor3d("DarkSlateGray"));
        using var rightRenderer = vtkRenderer.New();
        rightRenderer.SetViewport(0.5, 0, 1, 1);
        rightRenderer.UseHiddenLineRemovalOn();
        rightRenderer.AddActor(resampledActor);
        rightRenderer.SetBackground(colors.GetColor3d("DarkSlateGray"));

        using var window = vtkRenderWindow.New();
        window.SetWindowName("ResampleAppendedPolyData");
        window.SetSize(1024, 512);
        window.AddRenderer(rightRenderer);
        window.AddRenderer(leftRenderer);
        using var interactor = vtkRenderWindowInteractor.New();
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        interactor.SetRenderWindow(window);

        leftRenderer.ResetCamera();
        rightRenderer.SetActiveCamera(leftRenderer.GetActiveCamera());
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
