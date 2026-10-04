# PointPicker 移植说明

- 官方源码：[PointPicker.cxx](https://examples.vtk.org/site/Cxx/Interaction/PointPicker/)
- 官方类重写 trackball camera 的左键处理；改为托管 observer 调用 `vtkPointPicker` 并报告最近数据点 ID 和拾取坐标，保留默认相机操作。
- 新增 `vtkPointPicker` 类型、`vtkPicker.Pick(x,y,z,renderer)`、`vtkPointPicker.GetPointId()`，并复用 `vtkAbstractPicker.GetPickPosition(Span<double>)`。设置拾取容差为 0.01。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/PointPicker`；需在不同缩放级别确认容差感知、命中和未命中结果。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_PointPicker\result.json。

