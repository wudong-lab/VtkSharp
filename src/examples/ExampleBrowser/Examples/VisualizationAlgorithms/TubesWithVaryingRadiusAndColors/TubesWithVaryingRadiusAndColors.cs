using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TubesWithVaryingRadiusAndColors", "VisualizationAlgorithms",
    Description = "Maps point radii and RGB colors independently onto a helical tube.",
    SourceFiles = new[] { "Examples/VisualizationAlgorithms/TubesWithVaryingRadiusAndColors/TubesWithVaryingRadiusAndColors.cs" })]
internal class TubesWithVaryingRadiusAndColors : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example maps separate point arrays to tube radius and color:
        // https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/TubesWithVaryingRadiusAndColors/
        const int pointCount = 256;
        const int cycles = 5;
        using var namedColors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        using var radii = vtkDoubleArray.New();
        radii.SetName("TubeRadius");
        using var colors = vtkUnsignedCharArray.New();
        colors.SetName("Colors");
        colors.SetNumberOfComponents(3);

        for (int i = 0; i < pointCount; i++)
        {
            var t = (double)i / (pointCount - 1);
            var angle = 2 * Math.PI * cycles * t;
            points.InsertNextPoint(2 * Math.Cos(angle), 2 * Math.Sin(angle), 10 * t);
            radii.InsertNextTuple1(0.1 + 0.4 * Math.Sin(Math.PI * t));
            colors.InsertNextTuple3(255 * t, 0, 255 * (1 - t));
        }

        using var lines = vtkCellArray.New();
        lines.InsertNextCell(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            lines.InsertCellPoint(i);
        }

        using var spiral = vtkPolyData.New();
        spiral.SetPoints(points);
        spiral.SetLines(lines);
        spiral.GetPointData().AddArray(radii);
        spiral.GetPointData().SetActiveScalars("TubeRadius");
        spiral.GetPointData().AddArray(colors);

        using var tube = vtkTubeFilter.New();
        tube.SetInputData(spiral);
        tube.SetNumberOfSides(8);
        tube.SetVaryRadiusToVaryRadiusByAbsoluteScalar();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(tube.GetOutputPort());
        mapper.ScalarVisibilityOn();
        mapper.SetScalarModeToUsePointFieldData();
        mapper.SelectColorArray("Colors");

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        var background = namedColors.GetColor3d("SteelBlue");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.GetActiveCamera().Azimuth(30);
        renderer.GetActiveCamera().Elevation(30);
        renderer.ResetCamera();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(500, 500);
        window.SetWindowName("TubesWithVaryingRadiusAndColors");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "TubesWithVaryingRadiusAndColors", screenshotPath);
    }
}
