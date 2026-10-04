using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BatchedNodeLabels", "ExtraExamples",
    Description = "Measures non-overlapping labels on a 100,000-node 3D grid; hides labels during interaction.",
    SourceFiles = new[] { "ExtraExamples/BatchedNodeLabels/BatchedNodeLabels.cs" })]
internal sealed class BatchedNodeLabels : ISmokeExample
{
    private const int Columns = 10;
    private const int Rows = 100;
    private const int Layers = 100;
    private const double ColumnSpacing = 5;

    private const int LabelFontSize = 16;

    // 每个标签四周各留 4 像素；两个标签的留白相加，形成约 8 像素的最小间距。
    private const int LabelPadding = 4;

    // 用 2×2 像素网格近似碰撞矩形。整格占用会略微多剔除一些标签，换取简单快速的判断。
    private const int GridCellSize = 2;

    // 按数字位数估算宽度、按字号估算高度，适用于本例的短整数标签，并非精确字形测量。
    private const double LabelWidthPerDigit = 0.7;
    private const double LabelHeightInFontSizes = 1.3;

    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        var meshBuildTimer = Stopwatch.StartNew();
        // 同时保留托管坐标和 VTK 点集：前者用于筛选计算，后者用于杆系渲染。
        var worldCoordinates = new double[Columns * Rows * Layers * 3];
        using var points = vtkPoints.New();
        using var lines = vtkCellArray.New();
        var elementCount = 0;

        for (var x = 0; x < Columns; x++)
        {
            for (var y = 0; y < Rows; y++)
            {
                for (var z = 0; z < Layers; z++)
                {
                    // 三维索引展平为节点索引，Z 方向连续；显示编号为 pointId + 1。
                    var pointId = (x * Rows + y) * Layers + z;
                    var offset = pointId * 3;
                    worldCoordinates[offset] = x * ColumnSpacing;
                    worldCoordinates[offset + 1] = y;
                    worldCoordinates[offset + 2] = z;
                    points.InsertNextPoint(x * ColumnSpacing, y, z);
                    // 只连接三个正方向的相邻节点，避免重复创建同一杆单元。
                    if (x + 1 < Columns)
                        AddLine(pointId, pointId + Rows * Layers);
                    if (y + 1 < Rows)
                        AddLine(pointId, pointId + Layers);
                    if (z + 1 < Layers)
                        AddLine(pointId, pointId + 1);
                }
            }
        }

        void AddLine(int first, int second)
        {
            lines.InsertNextCell(2);
            lines.InsertCellPoint(first);
            lines.InsertCellPoint(second);
            elementCount++;
        }

        using var mesh = vtkPolyData.New();
        mesh.SetPoints(points);
        mesh.SetLines(lines);
        meshBuildTimer.Stop();

        using var meshMapper = vtkPolyDataMapper.New();
        meshMapper.SetInputData(mesh);
        using var meshActor = vtkActor.New();
        meshActor.SetMapper(meshMapper);
        meshActor.GetProperty().SetColor(VtkColor3d.Red);
        meshActor.GetProperty().SetLineWidth(0.5f);

        using var labelMapper = vtkOpenGLBatchedLabeledDataMapper.New();
        // 批量 mapper 的文字锚点独立于 vtkTextProperty 对齐设置，必须显式指定左下角。
        labelMapper.SetTextAnchor(0); // vtkBatchedLabeledDataMapper::LowerLeft
        labelMapper.SetLabelModeToLabelScalars();
        labelMapper.SetLabelFormat("{:.0f}");
        using var labelTextProperty = labelMapper.GetLabelTextProperty();
        labelTextProperty.SetColor(0, 1, 0);
        labelTextProperty.SetFontSize(LabelFontSize);
        labelTextProperty.SetJustificationToLeft();
        labelTextProperty.SetVerticalJustificationToBottom();
        labelTextProperty.ShadowOff();

        using var labels = vtkActor2D.New();
        labels.SetMapper(labelMapper);
        labels.VisibilityOff();

        using var renderer = vtkRenderer.New();
        renderer.AddActor(meshActor);
        renderer.SetBackground(0, 0, 0);

        using var labelRenderer = vtkRenderer.New();
        labelRenderer.SetLayer(1);
        // 标签放在上层 renderer，与模型共享相机；清除深度缓冲，避免被杆单元遮住。
        // 上层不接收交互，使相机交互和裁剪范围更新由持有模型包围盒的场景 renderer 负责。
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
        renderer.ResetCamera();

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);

        // 先完成一次渲染，使 native 视口尺寸有效，再按实际视口投影节点。
        window.Render();
        renderer.ResetCamera();
        camera.Azimuth(30);
        camera.Elevation(20);
        renderer.ResetCameraClippingRange();
        window.Render();
        var firstRefresh = RefreshLabels();

        LabelRefreshMetrics RefreshLabels()
        {
            // 一次刷新分为候选筛选、VTK 标签数据构造、渲染三个阶段，分别记录耗时。
            // 托管分配统计仅包含当前线程，不包含 VTK native 对象和 GPU 内存。
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var selected = SelectVisibleLabels(worldCoordinates, camera, renderer, out var selection);
            var selectionMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            using var input = CreateLabelInput(worldCoordinates, selected);
            // 只在 C# 中筛选一次，mapper 直接绘制全部选中标签。
            // 若再用 vtkLabelPlacementMapper 筛选，被二次剔除的标签仍占着 C# 碰撞网格，会留下空白。
            labelMapper.SetInputData(input);
            labels.VisibilityOn();
            var inputMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            window.Render();
            var renderMs = timer.Elapsed.TotalMilliseconds;
            var metrics = new LabelRefreshMetrics(selection.Candidates, selected.Length, selectionMs,
                selection.ProjectionMs, selection.SortMs, selection.CollisionMs, inputMs, renderMs,
                GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
            window.SetWindowName($"100,000 nodes | {selected.Length} labels | "
                                 + $"select {selectionMs:F1} ms | input {inputMs:F1} ms | render {renderMs:F1} ms");
            Debug.WriteLine(metrics);
            return metrics;
        }

        if (screenshotPath is not null)
        {
            // 截图模式附带固定视角性能采样：先预热，再统计多次刷新，首次刷新单独记录。
            // Render() 计时包含驱动提交和呈现等待，不能视为独立 GPU 耗时。
            const int warmups = 10;
            const int repetitions = 30;
            var results = new List<object>();
            foreach (var view in new[]
                     {
                         (Name: "Initial", Azimuth: 0.0, Elevation: 0.0, Zoom: 1.0),
                         (Name: "Rotated", Azimuth: 35.0, Elevation: 20.0, Zoom: 1.0),
                         (Name: "Zoomed", Azimuth: 0.0, Elevation: 0.0, Zoom: 2.0),
                     })
            {
                camera.Azimuth(view.Azimuth);
                camera.Elevation(view.Elevation);
                camera.Zoom(view.Zoom);
                renderer.ResetCameraClippingRange();
                for (var i = 0; i < warmups; i++)
                    RefreshLabels();
                var samples = Enumerable.Range(0, repetitions).Select(_ => RefreshLabels()).ToArray();
                // 同一视角关闭标签，测量单独绘制杆系的耗时，作为渲染对照。
                labels.VisibilityOff();
                for (var i = 0; i < warmups; i++)
                    window.Render();
                var sceneRenderMs = new double[repetitions];
                for (var i = 0; i < repetitions; i++)
                {
                    var timer = Stopwatch.StartNew();
                    window.Render();
                    sceneRenderMs[i] = timer.Elapsed.TotalMilliseconds;
                }

                results.Add(new
                {
                    view.Name,
                    Candidates = samples[0].Candidates,
                    Labels = samples[0].Labels,
                    Selection = Summarize(samples.Select(item => item.SelectionMs)),
                    Projection = Summarize(samples.Select(item => item.ProjectionMs)),
                    Sort = Summarize(samples.Select(item => item.SortMs)),
                    Collision = Summarize(samples.Select(item => item.CollisionMs)),
                    Input = Summarize(samples.Select(item => item.InputMs)),
                    Render = Summarize(samples.Select(item => item.RenderMs)),
                    Total = Summarize(samples.Select(item => item.SelectionMs + item.InputMs + item.RenderMs)),
                    SceneOnlyRender = Summarize(sceneRenderMs),
                    ManagedAllocatedBytes = samples.Average(item => item.ManagedAllocatedBytes),
                    Samples = samples,
                });
                // 按相反顺序撤销本轮相机变换，让各测试视角都从相同的初始状态出发。
                camera.Zoom(1 / view.Zoom);
                camera.Elevation(-view.Elevation);
                camera.Azimuth(-view.Azimuth);
            }

            renderer.ResetCameraClippingRange();
            RefreshLabels();
            var reportPath = Path.Combine(Path.GetDirectoryName(screenshotPath)!, "performance.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new
            {
                Nodes = Columns * Rows * Layers, Elements = elementCount,
                Grid = new[] { Columns, Rows, Layers }, Spacing = new[] { ColumnSpacing, 1.0, 1.0 },
                Viewport = new[] { 1000, 700 },
                LabelFontSize, LabelPadding, GridCellSize, Warmups = warmups, Repetitions = repetitions,
                MeshBuildMs = meshBuildTimer.Elapsed.TotalMilliseconds, FirstRefresh = firstRefresh,
                Runtime = RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
                Environment.ProcessorCount, Results = results,
                TimingNote = "Milliseconds; Render is wall time of window.Render(), not isolated GPU time. "
                             + "Managed allocation excludes native/GPU memory. MeshBuild includes per-element P/Invoke calls.",
            }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Performance report: {reportPath}");
        }

        var pendingTimerId = 0;

        // 交互开始：隐藏标签，并取消上一次尚未执行的刷新，避免快速操作时显示过期标签。
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
        // 交互结束：延迟 200 ms 刷新；连续缩放等操作会重新计时，减少重复计算。
        using var endObserver = interactor.AddObserver(
            vtkCommand.EndInteractionEvent,
            (_, _, _, _) =>
            {
                if (pendingTimerId != 0)
                    interactor.DestroyTimer(pendingTimerId);
                pendingTimerId = interactor.CreateOneShotTimer(200);
            });
        // 只响应本例最后创建的定时器。刷新仍在 VTK 交互线程执行，并非后台异步渲染。
        // observer 保留到交互循环结束，保证托管回调在 native 持有期间有效。
        using var timerObserver = interactor.AddTimerEventObserver(args =>
        {
            if (args.TimerId != pendingTimerId)
                return;

            pendingTimerId = 0;
            RefreshLabels();
        });

        ExampleRenderSupport.Finish(window, interactor, "BatchedNodeLabels", screenshotPath);
    }

    private static int[] SelectVisibleLabels(
        double[] worldCoordinates, vtkCamera camera, vtkRenderer renderer, out SelectionMetrics metrics)
    {
        var timer = Stopwatch.StartNew();
        // 步骤 1：取得当前视口和相机的“世界坐标 → 裁剪坐标”矩阵。
        // 本例 renderer 占满窗口，视口原点为 (0, 0)，没有额外的分屏偏移。
        Span<int> viewportWidth = stackalloc int[1];
        Span<int> viewportHeight = stackalloc int[1];
        renderer.GetTiledSize(viewportWidth, viewportHeight);
        var width = viewportWidth[0];
        var height = viewportHeight[0];

        var aspectRatio = (double)width / height;
        using var projection = camera.GetCompositeProjectionTransformMatrix(aspectRatio, -1, 1);
        // 矩阵元素每次刷新读取一次，后续全部节点投影在托管侧计算。
        var m00 = projection.GetElement(0, 0);
        var m01 = projection.GetElement(0, 1);
        var m02 = projection.GetElement(0, 2);
        var m03 = projection.GetElement(0, 3);
        var m10 = projection.GetElement(1, 0);
        var m11 = projection.GetElement(1, 1);
        var m12 = projection.GetElement(1, 2);
        var m13 = projection.GetElement(1, 3);
        var m20 = projection.GetElement(2, 0);
        var m21 = projection.GetElement(2, 1);
        var m22 = projection.GetElement(2, 2);
        var m23 = projection.GetElement(2, 3);
        var m30 = projection.GetElement(3, 0);
        var m31 = projection.GetElement(3, 1);
        var m32 = projection.GetElement(3, 2);
        var m33 = projection.GetElement(3, 3);
        var labelHeight = LabelFontSize * LabelHeightInFontSizes;
        var gridWidth = (width + GridCellSize - 1) / GridCellSize;
        var gridHeight = (height + GridCellSize - 1) / GridCellSize;
        // 一维数组表示屏幕占用网格，索引为 y * gridWidth + x；每次刷新重新建立。
        var occupied = new bool[gridWidth * gridHeight];
        var candidates = new List<LabelCandidate>(worldCoordinates.Length / 3);

        // 步骤 2：遍历全部节点，投影到屏幕，并过滤不满足视口条件的候选。
        for (var pointId = 0; pointId < worldCoordinates.Length / 3; pointId++)
        {
            var offset = pointId * 3;
            var x = worldCoordinates[offset];
            var y = worldCoordinates[offset + 1];
            var z = worldCoordinates[offset + 2];
            // 用齐次坐标 (x, y, z, 1) 乘矩阵；透视投影后需要除以 W。
            var clipX = m00 * x + m01 * y + m02 * z + m03;
            var clipY = m10 * x + m11 * y + m12 * z + m13;
            var clipZ = m20 * x + m21 * y + m22 * z + m23;
            var clipW = m30 * x + m31 * y + m32 * z + m33;
            // 排除 W 无效或位于透视相机后方的点，避免透视除法无效。
            if (clipW <= 0)
                continue;

            var normalizedX = clipX / clipW;
            var normalizedY = clipY / clipW;
            var normalizedZ = clipZ / clipW;
            // 本次矩阵将近、远裁剪平面映射到 -1、1，超出范围的节点无需显示。
            if (normalizedZ < -1 || normalizedZ > 1)
                continue;

            // 将归一化坐标 [-1, 1] 转成像素坐标；VTK 屏幕坐标以左下角为原点。
            var screenX = (normalizedX + 1) * width * 0.5;
            var screenY = (normalizedY + 1) * height * 0.5;
            var labelWidth = CountDigits(pointId + 1) * LabelFontSize * LabelWidthPerDigit;

            // 与 mapper 的左下角锚点一致：文字向右、向上展开，再在四周增加碰撞留白。
            var left = screenX - LabelPadding;
            var right = screenX + labelWidth + LabelPadding;
            var bottom = screenY - LabelPadding;
            var top = screenY + labelHeight + LabelPadding;
            // 保守处理边界：要求文字连同留白完整位于视口内，不显示部分截断的标签。
            if (left < 0 || right >= width || bottom < 0 || top >= height)
                continue;

            candidates.Add(new LabelCandidate(pointId, normalizedZ, left, right, bottom, top));
        }

        var projectionMs = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        // 步骤 3：按投影深度从近到远排序，让较近节点优先占用屏幕空间。
        // 深度相同按节点索引排序，保证结果稳定；这里比较的是视方向深度，而非到相机的欧氏距离。
        candidates.Sort(static (left, right) =>
        {
            var depthOrder = left.Depth.CompareTo(right.Depth);
            return depthOrder != 0 ? depthOrder : left.PointId.CompareTo(right.PointId);
        });

        var sortMs = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        var selected = new List<int>(Math.Min(candidates.Count, gridWidth * gridHeight));
        // 步骤 4：按优先级贪心接纳标签，不设显示数量上限。
        // 该方法保留深度优先规则，但不保证得到全局最多标签的排布。
        foreach (var candidate in candidates)
        {
            // 将带留白的矩形映射到网格单元；边界整格占用，使判断略偏保守。
            var minX = (int)Math.Floor(candidate.Left / GridCellSize);
            var maxX = (int)Math.Floor(candidate.Right / GridCellSize);
            var minY = (int)Math.Floor(candidate.Bottom / GridCellSize);
            var maxY = (int)Math.Floor(candidate.Top / GridCellSize);
            var collision = false;

            // 任意覆盖单元已占用就立即拒绝，无需逐一比较已有标签的矩形。
            for (var y = minY; y <= maxY && !collision; y++)
            {
                var rowOffset = y * gridWidth;
                for (var x = minX; x <= maxX; x++)
                {
                    if (!occupied[rowOffset + x])
                        continue;
                    collision = true;
                    break;
                }
            }

            if (collision)
                continue;

            // 只有接纳的标签才标记网格；被拒绝的候选不会阻挡后续标签。
            for (var y = minY; y <= maxY; y++)
            {
                var rowOffset = y * gridWidth;
                for (var x = minX; x <= maxX; x++)
                    occupied[rowOffset + x] = true;
            }

            selected.Add(candidate.PointId);
        }

        var pointIds = selected.ToArray();
        metrics = new SelectionMetrics(candidates.Count, projectionMs, sortMs, timer.Elapsed.TotalMilliseconds);
        return pointIds;
    }

    private static vtkPolyData CreateLabelInput(double[] worldCoordinates, int[] pointIds)
    {
        // 步骤 5：只为筛选出的少量节点建立 VTK 输入，全部杆系数据不需要重建。
        using var points = vtkPoints.New();
        using var labels = vtkDoubleArray.New();
        labels.SetName("NodeId");

        foreach (var pointId in pointIds)
        {
            var offset = pointId * 3;
            points.InsertNextPoint(
                worldCoordinates[offset], worldCoordinates[offset + 1], worldCoordinates[offset + 2]);
            // 子集中的顺序可能改变，因此编号必须取原始节点索引，不能使用子集索引。
            labels.InsertNextTuple1(pointId + 1);
        }

        var data = vtkPolyData.New();
        data.SetPoints(points);
        // 使用数值标量保存节点编号，mapper 根据 SetLabelFormat 格式化为整数文字。
        // VTK 数据对象持有点集和标量的引用，局部 wrapper 释放后输入仍然有效。
        data.GetPointData().SetScalars(labels);
        return data;
    }

    private static int CountDigits(int value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    private static TimingSummary Summarize(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return new TimingSummary(sorted[0], (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2,
            sorted.Average(), sorted[^1]);
    }

    private readonly record struct TimingSummary(double MinMs, double MedianMs, double MeanMs, double MaxMs);

    private readonly record struct LabelRefreshMetrics(
        int Candidates,
        int Labels,
        double SelectionMs,
        double ProjectionMs,
        double SortMs,
        double CollisionMs,
        double InputMs,
        double RenderMs,
        long ManagedAllocatedBytes);

    private readonly record struct SelectionMetrics(
        int Candidates,
        double ProjectionMs,
        double SortMs,
        double CollisionMs);

    private readonly record struct LabelCandidate(
        int PointId,
        double Depth,
        double Left,
        double Right,
        double Bottom,
        double Top);
}