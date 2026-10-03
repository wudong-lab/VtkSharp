using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Hexahedron", "GeometricObjects", Description = "Constructs and renders an eight-node hexahedral cell.", SourceFiles = new[] { "Examples/GeometricObjects/Hexahedron/Hexahedron.cs" })]
internal sealed class Hexahedron : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/GeometricObjects/Hexahedron/
        using var colors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        foreach (var point in new[] { (0d, 0d, 0d), (1d, 0d, 0d), (1d, 1d, 0d), (0d, 1d, 0d), (0d, 0d, 1d), (1d, 0d, 1d), (1d, 1d, 1d), (0d, 1d, 1d) })
            points.InsertNextPoint(point.Item1, point.Item2, point.Item3);

        using var cell = vtkHexahedron.New();
        for (var i = 0; i < 8; i++) cell.GetPointIds().SetId(i, i);
        using var grid = vtkUnstructuredGrid.New();
        grid.SetPoints(points);
        grid.InsertNextCell(12, cell.GetPointIds()); // VTK_HEXAHEDRON
        using var mapper = vtkDataSetMapper.New();
        mapper.SetInputData(grid);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(colors.GetColor3d("PeachPuff"));
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("Hexahedron");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(actor);
        renderer.SetBackground(0.2, 0.3, 0.4);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Azimuth(30);
        renderer.GetActiveCamera().Elevation(30);
        ExampleRenderSupport.Finish(window, interactor, "Hexahedron", screenshotPath);
    }
}
