using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("DataSetSurfaceFilter", "PolyData", Description = "Extracts and renders the exterior surface of a tetrahedral volume mesh.", SourceFiles = new[] { "Examples/PolyData/DataSetSurfaceFilter/DataSetSurfaceFilter.cs" })]
internal sealed class DataSetSurfaceFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/PolyData/DataSetSurfaceFilter/
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0); points.InsertNextPoint(1, 0, 0);
        points.InsertNextPoint(0, 1, 0); points.InsertNextPoint(0, 0, 1);
        using var tetra = vtkTetra.New();
        for (var i = 0; i < 4; ++i) tetra.GetPointIds().SetId(i, i);
        using var grid = vtkUnstructuredGrid.New();
        grid.SetPoints(points);
        grid.InsertNextCell(10, tetra.GetPointIds()); // VTK_TETRA
        using var surface = vtkDataSetSurfaceFilter.New();
        surface.SetInputData(grid);
        using var colors = vtkNamedColors.New();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(surface.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(colors.GetColor3d("PeachPuff"));
        actor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("DataSetSurfaceFilter");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        ExampleRenderSupport.Finish(window, interactor, "DataSetSurfaceFilter", screenshotPath);
    }
}
