# ShiftAndControl 移植说明

- 官方源码：[ShiftAndControl.cxx](https://examples.vtk.org/site/Cxx/Interaction/ShiftAndControl/)
- 官方类继承 `vtkInteractorStyleTrackballActor` 并重写左键处理；改用 interactor 低优先级 observer 读取托管事件参数中的 Shift、Ctrl 和 Alt 状态，同时保留 actor trackball 默认行为。
- 为使用官方交互模式增加 `vtkInteractorStyleTrackballActor` 类型绑定，未新增方法。
- 构建与截图 smoke 已通过；仍需人工测试无修饰键、Shift、Ctrl、Alt 点击及拖动 Actor。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_ShiftAndControl\result.json。

