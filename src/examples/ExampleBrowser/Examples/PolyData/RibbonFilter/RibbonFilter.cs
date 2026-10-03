using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("RibbonFilter", "PolyData",
    Description = "Generates a visible strip surface around a spatial centerline.",
    SourceFiles = new[] { "Examples/PolyData/RibbonFilter/RibbonFilter.cs" })]
internal class RibbonFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example generates a ribbon around a helical polyline:
        // https://examples.vtk.org/site/Cxx/PolyData/RibbonFilter/
        const int pointCount = 256;
        using var points = vtkPoints.New();
        for (int i = 0; i < pointCount; i++)
        {
            var t = (double)i / (pointCount - 1);
            var angle = 2 * Math.PI * 3 * t;
            points.InsertNextPoint(2 * Math.Cos(angle), 2 * Math.Sin(angle), 10 * t);
        }
        using var lines = vtkCellArray.New();
        lines.InsertNextCell(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            lines.InsertCellPoint(i);
        }
        using var centerline = vtkPolyData.New();
        centerline.SetPoints(points);
        centerline.SetLines(lines);

        using var lineMapper = vtkPolyDataMapper.New();
        lineMapper.SetInputData(centerline);
        using var lineActor = vtkActor.New();
        lineActor.SetMapper(lineMapper);
        using var colors = vtkNamedColors.New();
        var lineColor = colors.GetColor3d("Tomato");
        lineActor.GetProperty().SetColor(lineColor.R, lineColor.G, lineColor.B);
        lineActor.GetProperty().SetLineWidth(3);

        using var ribbon = vtkRibbonFilter.New();
        ribbon.SetInputData(centerline);
        ribbon.SetWidth(0.4);
        using var ribbonMapper = vtkPolyDataMapper.New();
        ribbonMapper.SetInputConnection(ribbon.GetOutputPort());
        using var ribbonActor = vtkActor.New();
        ribbonActor.SetMapper(ribbonMapper);
        var surfaceColor = colors.GetColor3d("AliceBlue");
        ribbonActor.GetProperty().SetColor(surfaceColor.R, surfaceColor.G, surfaceColor.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(ribbonActor);
        renderer.AddActor(lineActor);
        var background = colors.GetColor3d("SteelBlue");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.GetActiveCamera().Azimuth(40);
        renderer.GetActiveCamera().Elevation(30);
        renderer.ResetCamera();
        renderer.ResetCameraClippingRange();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("RibbonFilter");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "RibbonFilter", screenshotPath);
    }
}
