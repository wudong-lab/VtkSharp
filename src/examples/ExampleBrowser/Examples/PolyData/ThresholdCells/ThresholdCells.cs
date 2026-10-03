using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ThresholdCells", "PolyData", Description = "Selects polygon cells using a cell-associated integer index array.", SourceFiles = new[] { "Examples/PolyData/ThresholdCells/ThresholdCells.cs" })]
internal sealed class ThresholdCells : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0); points.InsertNextPoint(1, 0, 0); points.InsertNextPoint(0, 1, 0);
        points.InsertNextPoint(1, 1, 0); points.InsertNextPoint(2, 1, 0); points.InsertNextPoint(2, 0, 0);
        using var polys = vtkCellArray.New();
        foreach (var tri in new[] { new long[] { 0, 1, 2 }, new long[] { 1, 3, 2 }, new long[] { 3, 4, 5 } })
        { using var ids = vtkIdList.New(); ids.SetNumberOfIds(3); for (var i = 0; i < 3; ++i) ids.SetId(i, tri[i]); polys.InsertNextCell(ids); }
        using var values = vtkIntArray.New(); values.SetName("index"); values.SetNumberOfTuples(3); values.SetComponent(0, 0, 0); values.SetComponent(1, 0, 1); values.SetComponent(2, 0, 2);
        using var mesh = vtkPolyData.New(); mesh.SetPoints(points); mesh.SetPolys(polys); mesh.GetCellData().AddArray(values);
        using var threshold = vtkThreshold.New(); threshold.SetInputDataObject(0, mesh); threshold.SetLowerThreshold(1); threshold.SetThresholdFunction(1); // vtkThreshold::THRESHOLD_LOWER
        threshold.SetInputArrayToProcess(0, 0, 0, 1, "index");
        using var mapper = vtkDataSetMapper.New(); mapper.SetInputConnection(threshold.GetOutputPort());
        using var actor = vtkActor.New(); actor.SetMapper(mapper); actor.GetProperty().SetColor(colors.GetColor3d("Tomato")); actor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("ThresholdCells");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("SlateGray")); renderer.ResetCamera();
        ExampleRenderSupport.Finish(window, interactor, "ThresholdCells", screenshotPath);
    }
}
