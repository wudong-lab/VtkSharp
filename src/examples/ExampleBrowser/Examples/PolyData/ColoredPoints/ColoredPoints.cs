using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColoredPoints", "PolyData",
    Description = "Displays three independent vertices with RGB point colors.",
    SourceFiles = new[] { "Examples/PolyData/ColoredPoints/ColoredPoints.cs" })]
internal sealed class ColoredPoints : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/ColoredPoints/
        using var points = vtkPoints.New();
        points.InsertNextPoint(-1, 0, 0);
        points.InsertNextPoint(0, 1, 0);
        points.InsertNextPoint(1, 0, 0);
        using var pointCloud = vtkPolyData.New();
        pointCloud.SetPoints(points);
        using var glyphs = vtkVertexGlyphFilter.New();
        glyphs.SetInputData(pointCloud);
        using var colors = vtkUnsignedCharArray.New();
        colors.SetNumberOfComponents(3);
        colors.InsertNextTuple3(255, 0, 0);
        colors.InsertNextTuple3(0, 255, 0);
        colors.InsertNextTuple3(0, 0, 255);
        pointCloud.GetPointData().SetScalars(colors);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(glyphs.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetPointSize(20);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ColoredPoints");
        window.SetSize(500, 400);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ColoredPoints", screenshotPath);
    }
}
