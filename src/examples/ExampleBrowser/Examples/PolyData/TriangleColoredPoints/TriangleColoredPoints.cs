using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("TriangleColoredPoints", "PolyData",
    Description = "Interpolates three RGB point colors across a triangle.",
    SourceFiles = new[] { "Examples/PolyData/TriangleColoredPoints/TriangleColoredPoints.cs" })]
internal sealed class TriangleColoredPoints : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/TriangleColoredPoints/
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0);
        points.InsertNextPoint(1, 0, 0);
        points.InsertNextPoint(0.5, 1, 0);
        using var triangle = vtkTriangle.New();
        triangle.GetPointIds().SetId(0, 0);
        triangle.GetPointIds().SetId(1, 1);
        triangle.GetPointIds().SetId(2, 2);
        using var cells = vtkCellArray.New();
        cells.InsertNextCell(triangle);
        using var colors = vtkUnsignedCharArray.New();
        colors.SetNumberOfComponents(3);
        colors.InsertNextTuple3(255, 0, 0);
        colors.InsertNextTuple3(0, 255, 0);
        colors.InsertNextTuple3(0, 0, 255);
        using var mesh = vtkPolyData.New();
        mesh.SetPoints(points);
        mesh.SetPolys(cells);
        mesh.GetPointData().SetScalars(colors);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputData(mesh);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("TriangleColoredPoints");
        window.SetSize(500, 400);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "TriangleColoredPoints", screenshotPath);
    }
}
