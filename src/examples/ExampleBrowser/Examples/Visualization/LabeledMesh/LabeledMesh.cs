using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("LabeledMesh", "Visualization",
    Description = "Displays business node and element identifiers on a small line mesh.",
    SourceFiles = new[] { "Examples/Visualization/LabeledMesh/LabeledMesh.cs" })]
internal class LabeledMesh : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example labels visible mesh point and cell IDs:
        // https://examples.vtk.org/site/Cxx/Visualization/LabeledMesh/
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0);
        points.InsertNextPoint(2, 0, 0);
        points.InsertNextPoint(2, 1, 0);
        points.InsertNextPoint(0, 1, 0);

        using var lines = vtkCellArray.New();
        lines.InsertNextCell(2);
        lines.InsertCellPoint(0);
        lines.InsertCellPoint(1);
        lines.InsertNextCell(2);
        lines.InsertCellPoint(1);
        lines.InsertCellPoint(2);
        lines.InsertNextCell(2);
        lines.InsertCellPoint(2);
        lines.InsertCellPoint(3);

        using var nodeIds = vtkDoubleArray.New();
        nodeIds.SetName("NodeId");
        nodeIds.SetNumberOfTuples(4);
        nodeIds.SetTuple1(0, 101);
        nodeIds.SetTuple1(1, 205);
        nodeIds.SetTuple1(2, 309);
        nodeIds.SetTuple1(3, 412);

        using var elementIds = vtkDoubleArray.New();
        elementIds.SetName("ElementId");
        elementIds.SetNumberOfTuples(3);
        elementIds.SetTuple1(0, 7001);
        elementIds.SetTuple1(1, 7004);
        elementIds.SetTuple1(2, 7012);

        using var mesh = vtkPolyData.New();
        mesh.SetPoints(points);
        mesh.SetLines(lines);
        mesh.GetPointData().SetScalars(nodeIds);
        mesh.GetCellData().SetScalars(elementIds);

        using var meshMapper = vtkPolyDataMapper.New();
        meshMapper.SetInputData(mesh);
        using var meshActor = vtkActor.New();
        meshActor.SetMapper(meshMapper);
        meshActor.GetProperty().SetLineWidth(3);

        using var nodeLabelMapper = vtkLabeledDataMapper.New();
        nodeLabelMapper.SetInputData(mesh);
        nodeLabelMapper.SetLabelModeToLabelScalars();
        nodeLabelMapper.SetLabelFormat("%.0f");
        nodeLabelMapper.GetLabelTextProperty().SetColor(1, 0.85, 0.2);
        using var nodeLabels = vtkActor2D.New();
        nodeLabels.SetMapper(nodeLabelMapper);

        using var cellCenters = vtkCellCenters.New();
        cellCenters.SetInputData(mesh);
        using var elementLabelMapper = vtkLabeledDataMapper.New();
        elementLabelMapper.SetInputConnection(cellCenters.GetOutputPort());
        elementLabelMapper.SetLabelModeToLabelScalars();
        elementLabelMapper.SetLabelFormat("%.0f");
        elementLabelMapper.GetLabelTextProperty().SetColor(0.2, 1, 0.4);
        using var elementLabels = vtkActor2D.New();
        elementLabels.SetMapper(elementLabelMapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(meshActor);
        renderer.AddViewProp(nodeLabels);
        renderer.AddViewProp(elementLabels);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Zoom(0.7);
        renderer.ResetCameraClippingRange();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("Business node and element IDs");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "LabeledMesh", screenshotPath);
    }
}
