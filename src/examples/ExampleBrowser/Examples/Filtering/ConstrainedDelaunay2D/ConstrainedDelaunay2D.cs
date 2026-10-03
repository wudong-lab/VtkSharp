using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ConstrainedDelaunay2D", "Filtering", Description = "Triangulates a jittered planar point grid while preserving a polygonal hole boundary.", SourceFiles = new[] { "Examples/Filtering/ConstrainedDelaunay2D/ConstrainedDelaunay2D.cs" })]
internal sealed class ConstrainedDelaunay2D : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        for (var x = 0; x < 10; ++x) for (var y = 0; y < 10; ++y) points.InsertNextPoint(x, y, 0);
        using var input = vtkPolyData.New(); input.SetPoints(points);
        using var polygon = vtkPolygon.New();
        polygon.GetPointIds().SetNumberOfIds(10);
        var boundaryIds = new long[] { 22, 23, 24, 25, 35, 45, 44, 43, 42, 32 };
        for (var i = 0; i < boundaryIds.Length; i++) polygon.GetPointIds().SetId(i, boundaryIds[i]);
        using var cells = vtkCellArray.New(); cells.InsertNextCell(polygon);
        using var boundary = vtkPolyData.New(); boundary.SetPoints(points); boundary.SetPolys(cells);
        using var delaunay = vtkDelaunay2D.New(); delaunay.SetInputData(input); delaunay.SetSourceData(boundary);
        using var meshMapper = vtkPolyDataMapper.New(); meshMapper.SetInputConnection(delaunay.GetOutputPort());
        using var meshActor = vtkActor.New(); meshActor.SetMapper(meshMapper); meshActor.GetProperty().EdgeVisibilityOn();
        meshActor.GetProperty().SetEdgeColor(colors.GetColor3d("Peacock"));
        using var boundaryMapper = vtkPolyDataMapper.New(); boundaryMapper.SetInputData(boundary);
        using var boundaryActor = vtkActor.New(); boundaryActor.SetMapper(boundaryMapper); boundaryActor.GetProperty().SetColor(colors.GetColor3d("Raspberry"));
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("ConstrainedDelaunay2D");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(meshActor); renderer.AddActor(boundaryActor); renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "ConstrainedDelaunay2D", screenshotPath);
    }
}
