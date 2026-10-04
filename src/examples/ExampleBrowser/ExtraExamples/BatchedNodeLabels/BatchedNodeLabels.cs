using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BatchedNodeLabels", "ExtraExamples",
    Description = "Places non-overlapping node labels by camera depth and hides them during interaction.",
    SourceFiles = new[] { "ExtraExamples/BatchedNodeLabels/BatchedNodeLabels.cs" })]
internal sealed class BatchedNodeLabels : ISmokeExample
{
    private const int Columns = 64;
    private const int Rows = 12;

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

        using var priorities = vtkDoubleArray.New();
        priorities.SetName("ViewPriority");
        priorities.SetNumberOfTuples(Columns * Rows);
        mesh.GetPointData().AddArray(priorities);

        using var meshMapper = vtkPolyDataMapper.New();
        meshMapper.SetInputData(mesh);
        using var meshActor = vtkActor.New();
        meshActor.SetMapper(meshMapper);
        meshActor.GetProperty().SetColor(0.75, 0.8, 0.85);
        meshActor.GetProperty().SetLineWidth(2);

        using var labelTextProperty = vtkTextProperty.New();
        labelTextProperty.SetColor(1, 0.85, 0.2);
        labelTextProperty.SetFontSize(12);

        using var hierarchy = vtkPointSetToLabelHierarchy.New();
        hierarchy.SetInputDataObject(0, mesh);
        hierarchy.SetLabelArrayName("NodeId");
        hierarchy.SetPriorityArrayName("ViewPriority");
        hierarchy.SetTextProperty(labelTextProperty);

        using var labels = vtkActor2D.New();

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

        UpdatePriorities(points, priorities, camera);
        hierarchy.Update();
        using var initialLabelMapper = CreateLabelMapper(hierarchy);
        labels.SetMapper(initialLabelMapper);

        var pendingTimerId = 0;

        using var startObserver = interactor.AddObserver(
            vtkCommand.StartInteractionEvent,
            (_, _, _, _) =>
            {
                labels.VisibilityOff();
                if (pendingTimerId != 0)
                {
                    interactor.DestroyTimer(pendingTimerId);
                    pendingTimerId = 0;
                }
            });
        using var endObserver = interactor.AddObserver(
            vtkCommand.EndInteractionEvent,
            (_, _, _, _) =>
            {
                if (pendingTimerId != 0)
                    interactor.DestroyTimer(pendingTimerId);
                pendingTimerId = interactor.CreateOneShotTimer(200);
            });
        using var timerObserver = interactor.AddTimerEventObserver(args =>
        {
            if (args.TimerId != pendingTimerId)
                return;

            pendingTimerId = 0;
            UpdatePriorities(points, priorities, camera);
            hierarchy.Update();

            // Recreate the mapper so its temporal label cache cannot retain labels
            // that were nearest from the previous camera orientation.
            using var settledLabelMapper = CreateLabelMapper(hierarchy);
            labels.SetMapper(settledLabelMapper);
            labels.VisibilityOn();
            window.Render();
        });

        // Initialize the render window before the label placement mapper's first pass.
        window.Render();
        ExampleRenderSupport.Finish(window, interactor, "BatchedNodeLabels", screenshotPath);
    }

    private static vtkLabelPlacementMapper CreateLabelMapper(vtkPointSetToLabelHierarchy hierarchy)
    {
        var mapper = vtkLabelPlacementMapper.New();
        mapper.SetInputConnection(hierarchy.GetOutputPort());
        mapper.SetIteratorType(0); // vtkLabelHierarchy::FULL_SORT
        mapper.SetMaximumLabelFraction(1.0);
        mapper.UseDepthBufferOff();
        return mapper;
    }

    private static void UpdatePriorities(
        vtkPoints points, vtkDoubleArray priorities, vtkCamera camera)
    {
        var position = new double[3];
        var focalPoint = new double[3];
        camera.GetPosition(position);
        camera.GetFocalPoint(focalPoint);

        var directionX = focalPoint[0] - position[0];
        var directionY = focalPoint[1] - position[1];
        var directionZ = focalPoint[2] - position[2];
        var directionLength = Math.Sqrt(
            directionX * directionX + directionY * directionY + directionZ * directionZ);
        directionX /= directionLength;
        directionY /= directionLength;
        directionZ /= directionLength;

        var point = new double[3];
        for (var pointId = 0L; pointId < points.GetNumberOfPoints(); pointId++)
        {
            points.GetPoint(pointId, point);
            var depth = (point[0] - position[0]) * directionX
                + (point[1] - position[1]) * directionY
                + (point[2] - position[2]) * directionZ;
            priorities.SetTuple1(pointId, -depth);
        }
        priorities.Modified();
    }
}
