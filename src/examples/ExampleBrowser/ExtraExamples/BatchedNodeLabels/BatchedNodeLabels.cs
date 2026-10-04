using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BatchedNodeLabels", "ExtraExamples",
    Description = "Renders batched node labels and hides them while the camera is being manipulated.",
    SourceFiles = new[] { "ExtraExamples/BatchedNodeLabels/BatchedNodeLabels.cs" })]
internal sealed class BatchedNodeLabels : ISmokeExample
{
    private const int Columns = 24;
    private const int Rows = 8;

    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var points = vtkPoints.New();
        using var nodeIds = vtkDoubleArray.New();
        nodeIds.SetName("NodeId");
        nodeIds.SetNumberOfTuples(Columns * Rows);

        for (var x = 0; x < Columns; x++)
        {
            for (var y = 0; y < Rows; y++)
            {
                var z = Math.Sin(x * 0.22) * 0.5 + Math.Cos(y * 0.35) * 0.15;
                var pointId = x * Rows + y;
                points.InsertNextPoint(x, y * 0.7, z);
                nodeIds.SetTuple1(pointId, pointId + 1);
            }
        }

        using var lines = vtkCellArray.New();
        for (var x = 0; x < Columns; x++)
        {
            for (var y = 0; y < Rows; y++)
            {
                var pointId = x * Rows + y;
                if (x + 1 < Columns)
                {
                    lines.InsertNextCell(2);
                    lines.InsertCellPoint(pointId);
                    lines.InsertCellPoint(pointId + Rows);
                }
                if (y + 1 < Rows)
                {
                    lines.InsertNextCell(2);
                    lines.InsertCellPoint(pointId);
                    lines.InsertCellPoint(pointId + 1);
                }
            }
        }

        using var mesh = vtkPolyData.New();
        mesh.SetPoints(points);
        mesh.SetLines(lines);
        mesh.GetPointData().SetScalars(nodeIds);

        using var meshMapper = vtkPolyDataMapper.New();
        meshMapper.SetInputData(mesh);
        using var meshActor = vtkActor.New();
        meshActor.SetMapper(meshMapper);
        meshActor.GetProperty().SetColor(0.75, 0.8, 0.85);
        meshActor.GetProperty().SetLineWidth(2);

        using var labelMapper = vtkOpenGLBatchedLabeledDataMapper.New();
        labelMapper.SetInputData(mesh);
        labelMapper.SetLabelModeToLabelScalars();
        labelMapper.SetLabelFormat("{:.0f}");
        labelMapper.GetLabelTextProperty().SetColor(1, 0.85, 0.2);
        labelMapper.GetLabelTextProperty().SetFontSize(12);
        using var labels = vtkActor2D.New();
        labels.SetMapper(labelMapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(meshActor);
        renderer.SetBackground(0.12, 0.16, 0.2);
        renderer.ResetCamera();

        using var labelRenderer = vtkRenderer.New();
        labelRenderer.SetLayer(1);
        // Keep mouse interaction on the scene renderer, which owns the mesh bounds used for clipping.
        labelRenderer.InteractiveOff();
        labelRenderer.PreserveDepthBufferOff();
        using var camera = renderer.GetActiveCamera();
        labelRenderer.SetActiveCamera(camera);
        labelRenderer.AddViewProp(labels);

        using var window = vtkRenderWindow.New();
        window.SetNumberOfLayers(2);
        window.AddRenderer(renderer);
        window.AddRenderer(labelRenderer);
        window.SetSize(1000, 700);
        window.SetWindowName("Batched node labels");

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        using var startObserver = interactor.AddObserver(
            vtkCommand.StartInteractionEvent,
            (_, _, _, _) => labels.VisibilityOff());
        using var endObserver = interactor.AddObserver(
            vtkCommand.EndInteractionEvent,
            (_, _, _, _) => labels.VisibilityOn());

        ExampleRenderSupport.Finish(window, interactor, "BatchedNodeLabels", screenshotPath);
    }
}
