using System.IO;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("FitToHeightMap", "Meshes",
    Description = "Drapes a plane over DEM terrain using point projection and cell-average fitting strategies.",
    SourceFiles = new[] { "Examples/Meshes/FitToHeightMap/FitToHeightMap.cs" })]
internal sealed class FitToHeightMap : IExample
{
    public void Run()
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/Meshes/FitToHeightMap/
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "SainteHelens.dem");
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException("The SainteHelens DEM example data was not found.", dataPath);
        }

        using var colors = vtkNamedColors.New();

        using var ren0 = vtkRenderer.New();
        ren0.SetViewport(0.0, 0.0, 1.0 / 3.0, 1.0);
        SetBackground(ren0, colors, "Wheat");

        using var ren1 = vtkRenderer.New();
        ren1.SetViewport(1.0 / 3.0, 0.0, 2.0 / 3.0, 1.0);
        SetBackground(ren1, colors, "BurlyWood");

        using var ren2 = vtkRenderer.New();
        ren2.SetViewport(2.0 / 3.0, 0.0, 1.0, 1.0);
        SetBackground(ren2, colors, "Tan");

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.SetSize(1200, 400);
        renderWindow.AddRenderer(ren0);
        renderWindow.AddRenderer(ren1);
        renderWindow.AddRenderer(ren2);
        renderWindow.SetWindowName("FitToHeightMap");

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(renderWindow);

        using var lut = vtkLookupTable.New();
        lut.SetHueRange(0.6, 0.0);
        lut.SetSaturationRange(1.0, 0.0);
        lut.SetValueRange(0.5, 1.0);

        using var demReader = vtkDEMReader.New();
        demReader.SetFileName(dataPath);
        demReader.Update();

        using var dem = demReader.GetOutput();
        Span<double> scalarRange = stackalloc double[2];
        dem.GetScalarRange(scalarRange);
        Span<double> bounds = stackalloc double[6];
        dem.GetBounds(bounds);

        using var surface = vtkImageDataGeometryFilter.New();
        surface.SetInputConnection(demReader.GetOutputPort());
        surface.SetOutputTriangles(false);

        using var warp = vtkWarpScalar.New();
        warp.SetInputConnection(surface.GetOutputPort());
        warp.SetScaleFactor(1.0);
        warp.UseNormalOn();
        warp.SetNormal(0.0, 0.0, 1.0);
        warp.Update();

        using var demMapper = CreateMapper(warp.GetOutputPort(), lut, scalarRange);
        using var demActor = vtkActor.New();
        demActor.SetMapper(demMapper);

        var zLevel = bounds[5];
        using var plane = vtkPlaneSource.New();
        plane.SetOrigin(bounds[0], bounds[2], zLevel);
        plane.SetPoint1(bounds[1], bounds[2], zLevel);
        plane.SetPoint2(bounds[0], bounds[3], zLevel);
        plane.SetResolution(128, 128);
        plane.Update();

        using var probeDem = vtkProbeFilter.New();
        probeDem.SetSourceData(dem);
        probeDem.SetInputConnection(plane.GetOutputPort());
        probeDem.Update();

        using var pointFit = vtkFitToHeightMapFilter.New();
        pointFit.SetInputConnection(probeDem.GetOutputPort());
        pointFit.SetHeightMapConnection(demReader.GetOutputPort());
        pointFit.SetFittingStrategyToPointProjection();
        pointFit.UseHeightMapOffsetOn();

        using var pointMapper = CreateMapper(pointFit.GetOutputPort(), lut, scalarRange);
        using var pointActor = vtkActor.New();
        pointActor.SetMapper(pointMapper);

        using var cellFit = vtkFitToHeightMapFilter.New();
        cellFit.SetInputConnection(probeDem.GetOutputPort());
        cellFit.SetHeightMapConnection(demReader.GetOutputPort());
        cellFit.SetFittingStrategyToCellAverageHeight();
        cellFit.UseHeightMapOffsetOn();

        using var cellMapper = CreateMapper(cellFit.GetOutputPort(), lut, scalarRange);
        using var cellActor = vtkActor.New();
        cellActor.SetMapper(cellMapper);

        ren0.AddActor(demActor);
        ren1.AddActor(pointActor);
        ren2.AddActor(cellActor);

        using var camera = ren0.GetActiveCamera();
        camera.SetPosition(1.0, 0.0, 0.0);
        camera.SetFocalPoint(0.0, 1.0, 0.0);
        camera.SetViewUp(0.0, 0.0, 1.0);
        ren0.ResetCamera();
        camera.Azimuth(30.0);
        camera.Elevation(60.0);
        ren1.SetActiveCamera(camera);
        ren2.SetActiveCamera(camera);

        renderWindow.Render();
        interactor.Start();
    }

    private static vtkPolyDataMapper CreateMapper(
        vtkAlgorithmOutput output,
        vtkLookupTable lut,
        ReadOnlySpan<double> scalarRange)
    {
        var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(output);
        mapper.ScalarVisibilityOn();
        mapper.SetScalarRange(scalarRange);
        mapper.SetLookupTable(lut);
        return mapper;
    }

    private static void SetBackground(vtkRenderer renderer, vtkNamedColors colors, string colorName)
    {
        var color = colors.GetColor3d(colorName);
        renderer.SetBackground(color.R, color.G, color.B);
    }
}
