using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ClipUnstructuredGridWithPlane", "UnstructuredGrid", Description = "Clips a hexahedral volume mesh and displays both halves with solver cell IDs retained.", SourceFiles = new[] { "Examples/UnstructuredGrid/ClipUnstructuredGridWithPlane/ClipUnstructuredGridWithPlane.cs" })]
internal sealed class ClipUnstructuredGridWithPlane : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        for (var z = 0; z < 2; ++z) for (var y = 0; y < 2; ++y) for (var x = 0; x < 2; ++x) points.InsertNextPoint(x, y, z);
        using var ids = vtkIdList.New();
        ids.SetNumberOfIds(8);
        var connectivity = new long[] { 0, 1, 3, 2, 4, 5, 7, 6 };
        for (var i = 0; i < connectivity.Length; ++i) ids.SetId(i, connectivity[i]);
        using var grid = vtkUnstructuredGrid.New(); grid.SetPoints(points); grid.InsertNextCell(12, ids);
        using var solverIds = vtkIntArray.New(); solverIds.SetName("SolverCellId"); solverIds.SetNumberOfTuples(1); solverIds.SetComponent(0, 0, 42017); grid.GetCellData().AddArray(solverIds);
        using var plane = vtkPlane.New(); plane.SetOrigin(0.5, 0.5, 0.5); plane.SetNormal(1, 0, 0);
        using var clip = vtkTableBasedClipDataSet.New(); clip.SetInputDataObject(0, grid); clip.SetClipFunction(plane); clip.GenerateClippedOutputOn();
        using var insideMapper = vtkDataSetMapper.New(); insideMapper.SetInputConnection(clip.GetOutputPort()); insideMapper.ScalarVisibilityOff();
        using var insideActor = vtkActor.New(); insideActor.SetMapper(insideMapper); insideActor.GetProperty().SetColor(colors.GetColor3d("Banana")); insideActor.GetProperty().EdgeVisibilityOn();
        using var outsideMapper = vtkDataSetMapper.New(); outsideMapper.SetInputConnection(clip.GetOutputPort(1)); outsideMapper.ScalarVisibilityOff();
        using var outsideActor = vtkActor.New(); outsideActor.SetMapper(outsideMapper); outsideActor.GetProperty().SetColor(colors.GetColor3d("Tomato")); outsideActor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetSize(640, 480); window.SetWindowName("ClipUnstructuredGridWithPlane");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(insideActor); renderer.AddActor(outsideActor); renderer.SetBackground(colors.GetColor3d("Wheat")); renderer.ResetCamera();
        ExampleRenderSupport.Finish(window, interactor, "ClipUnstructuredGridWithPlane", screenshotPath);
    }
}
