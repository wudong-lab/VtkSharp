# WorldPointPicker 移植说明

- 官方源码：[WorldPointPicker.cxx](https://examples.vtk.org/site/Cxx/Interaction/WorldPointPicker/)
- 用低优先级托管左键 observer 调用 `vtkWorldPointPicker`，读取深度缓冲中鼠标位置对应的三维坐标；该点可能位于单元内部，不对应数据点 ID。
- 新增 `vtkWorldPointPicker` 类型和其三坐标 `Pick` 重载；坐标通过公共 `vtkAbstractPicker.GetPickPosition(Span<double>)` 复制到托管内存。默认相机样式继续处理按键。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/WorldPointPicker`；需人工在球面和背景点击，检查返回点及无拾取对象身份的语义。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_WorldPointPicker\result.json。

