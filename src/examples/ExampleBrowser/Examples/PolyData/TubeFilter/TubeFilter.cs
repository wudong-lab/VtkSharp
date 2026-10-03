using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TubeFilter", "PolyData",
    Description = "Creates a cylindrical surface around a centerline while retaining the centerline for comparison.",
    SourceFiles = new[] { "Examples/PolyData/TubeFilter/TubeFilter.cs" })]
internal class TubeFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example compares a centerline with its tube surface:
        // https://examples.vtk.org/site/Cxx/PolyData/TubeFilter/
        using var colors = vtkNamedColors.New();
        using var lineSource = vtkLineSource.New();
        lineSource.SetPoint1(1, 0, 0);
        lineSource.SetPoint2(0, 1, 0);

        using var lineMapper = vtkPolyDataMapper.New();
        lineMapper.SetInputConnection(lineSource.GetOutputPort());
        using var lineActor = vtkActor.New();
        lineActor.GetProperty().SetColor(colors.GetColor3d("Red").R,
            colors.GetColor3d("Red").G, colors.GetColor3d("Red").B);
        lineActor.SetMapper(lineMapper);

        using var tube = vtkTubeFilter.New();
        tube.SetInputConnection(lineSource.GetOutputPort());
        tube.SetRadius(0.025);
        tube.SetNumberOfSides(50);

        using var tubeMapper = vtkPolyDataMapper.New();
        tubeMapper.SetInputConnection(tube.GetOutputPort());
        using var tubeActor = vtkActor.New();
        tubeActor.GetProperty().SetOpacity(0.5);
        tubeActor.SetMapper(tubeMapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(tubeActor);
        renderer.AddActor(lineActor);
        var background = colors.GetColor3d("DarkSlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("TubeFilter");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "TubeFilter", screenshotPath);
    }
}
