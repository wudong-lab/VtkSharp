using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColorCells", "PolyData",
    Description = "Maps cell scalar values to colors on a small quad mesh.",
    SourceFiles = new[] { "Examples/PolyData/ColorCells/ColorCells.cs" })]
internal sealed class ColorCells : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/ColorCells/
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0); points.InsertNextPoint(1, 0, 0); points.InsertNextPoint(2, 0, 0);
        points.InsertNextPoint(0, 1, 0); points.InsertNextPoint(1, 1, 0); points.InsertNextPoint(2, 1, 0);
        using var cells = vtkCellArray.New();
        using var quad = vtkQuad.New();
        for (int x = 0; x < 2; x++)
        {
            quad.GetPointIds().SetId(0, x);
            quad.GetPointIds().SetId(1, x + 1);
            quad.GetPointIds().SetId(2, x + 4);
            quad.GetPointIds().SetId(3, x + 3);
            cells.InsertNextCell(quad);
        }
        using var values = vtkIntArray.New();
        values.SetNumberOfComponents(1);
        values.InsertNextTuple1(0);
        values.InsertNextTuple1(1);
        using var mesh = vtkPolyData.New();
        mesh.SetPoints(points);
        mesh.SetPolys(cells);
        mesh.GetCellData().SetScalars(values);
        using var lut = vtkLookupTable.New();
        lut.SetTableRange(0, 1);
        lut.Build();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputData(mesh);
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 1);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ColorCells");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ColorCells", screenshotPath);
    }
}
