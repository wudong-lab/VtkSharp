using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TubesFromSplines", "VisualizationAlgorithms",
    Description = "Fits a spline through control points and creates a tube with a smoothly varying radius.",
    SourceFiles = new[] { "Examples/VisualizationAlgorithms/TubesFromSplines/TubesFromSplines.cs" })]
internal class TubesFromSplines : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example fits a spline and interpolates tube radii:
        // https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/TubesFromSplines/
        using var controlPoints = vtkPoints.New();
        controlPoints.InsertNextPoint(1, 0, 0);
        controlPoints.InsertNextPoint(2, 0, 0);
        controlPoints.InsertNextPoint(3, 1, 0);
        controlPoints.InsertNextPoint(4, 1, 0);
        controlPoints.InsertNextPoint(5, 0, 0);
        controlPoints.InsertNextPoint(6, 0, 0);

        using var spline = vtkParametricSpline.New();
        spline.SetPoints(controlPoints);
        using var source = vtkParametricFunctionSource.New();
        source.SetParametricFunction(spline);
        source.SetUResolution((int)(10 * controlPoints.GetNumberOfPoints()));
        source.Update();
        var centerline = source.GetOutput();
        var pointCount = centerline.GetNumberOfPoints();

        using var radii = vtkDoubleArray.New();
        radii.SetName("TubeRadius");
        radii.SetNumberOfTuples(pointCount);
        for (long i = 0; i < pointCount; i++)
        {
            var t = (double)i / (pointCount - 1);
            radii.SetTuple1(i, 0.2 - 0.1 * t);
        }
        centerline.GetPointData().AddArray(radii);
        centerline.GetPointData().SetActiveScalars("TubeRadius");

        using var tube = vtkTubeFilter.New();
        tube.SetInputData(centerline);
        tube.SetNumberOfSides(20);
        tube.SetVaryRadiusToVaryRadiusByAbsoluteScalar();
        using var lineMapper = vtkPolyDataMapper.New();
        lineMapper.SetInputData(centerline);
        using var tubeMapper = vtkPolyDataMapper.New();
        tubeMapper.SetInputConnection(tube.GetOutputPort());
        using var lineActor = vtkActor.New();
        lineActor.SetMapper(lineMapper);
        lineActor.GetProperty().SetLineWidth(3);
        using var tubeActor = vtkActor.New();
        tubeActor.SetMapper(tubeMapper);
        tubeActor.GetProperty().SetOpacity(0.6);

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        renderer.UseHiddenLineRemovalOn();
        renderer.AddActor(lineActor);
        renderer.AddActor(tubeActor);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(640, 480);
        window.SetWindowName("TubesFromSplines");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "TubesFromSplines", screenshotPath);
    }
}
