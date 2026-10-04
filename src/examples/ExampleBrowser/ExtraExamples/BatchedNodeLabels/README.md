# 十万节点标签性能示例

网格包含 `10 × 100 × 100` 个节点，沿三个坐标轴连接相邻节点，共 288,000 个杆单元。 X、Y、Z 方向的节点间距分别为 5、1、1，网格尺寸为 `45 × 99 × 99`。

在 ExampleBrowser 中运行 `ExtraExamples/BatchedNodeLabels`。交互时隐藏标签，结束后等待 200 ms，重新筛选并绘制。窗口标题显示本次标签数量、筛选、数据构造和渲染耗时。

## 自动性能采样

在仓库根目录运行以下 PowerShell 命令：

```powershell
$outputDirectory = Join-Path $env:TEMP ("vtk-label-100k-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
dotnet run --project src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release -- --smoke ExtraExamples/BatchedNodeLabels --output $outputDirectory
```

输出目录必须是新目录。输出包含 `screenshot.png`、`result.json` 和 `performance.json`。

对初始、旋转、放大三个视角，分别预热 10 次、采样 30 次。报告保留原始数据，以及最小值、中位数、均值和最大值，单位为毫秒。

- `Projection`：读取投影矩阵、投影全部节点、视口裁剪、构造碰撞矩形。
- `Sort`：候选按深度排序。
- `Collision`：屏幕网格碰撞筛选、生成选中节点索引。
- `Input`：为选中节点构造 VTK 标签数据并更新 mapper 输入。
- `Render`：调用 `window.Render()` 的墙钟耗时。
- `Total`：筛选、标签输入更新、渲染的总耗时，不包含 200 ms 延迟。
- `SceneOnlyRender`：同一视角隐藏标签后的渲染耗时。
- `MeshBuildMs`：节点和杆单元数据构造耗时，包含逐元素 P/Invoke 调用。
- `FirstRefresh`：首次标签刷新，独立记录，不计入预热后的统计。
- `ManagedAllocatedBytes`：当前线程每次刷新分配的托管字节数，不包含 native 和 GPU 内存。

`Render` 包含驱动提交和呈现等待，不能当作独立 GPU 耗时，也不能用它单独推算交互帧率。各阶段中位数的和不一定等于总耗时中位数。此测试使用固定视角重复刷新；实际鼠标事件与延时触发仍需交互验证。
