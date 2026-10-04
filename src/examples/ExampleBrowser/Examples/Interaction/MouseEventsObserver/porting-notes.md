# MouseEventsObserver 移植说明

- 官方源码：[MouseEventsObserver.cxx](https://examples.vtk.org/site/Cxx/Interaction/MouseEventsObserver/)
- 实现使用 VtkSharp 托管鼠标和按键 observer；通过较低优先级在默认 trackball camera 处理后记录事件，不修改默认交互。
- `vtkCallbackCommand` 替换为托管委托；无额外绑定需求，observer handle 在渲染期间保持有效。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/MouseEventsObserver`；需人工点击左右键并按键，确认窗口仍可旋转、缩放和平移。
- 原始示例未定义鼠标中键回调，本移植也未新增该事件。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_MouseEventsObserver\result.json。

