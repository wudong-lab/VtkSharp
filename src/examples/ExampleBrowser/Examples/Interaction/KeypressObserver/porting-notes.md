# KeypressObserver 移植说明

- 官方源码：[KeypressObserver.cxx](https://examples.vtk.org/site/Cxx/Interaction/KeypressObserver/)
- `vtkCallbackCommand` 改为 `AddKeyPressEventObserver`，在托管事件参数中读取 `KeySym`。使用低优先级保留默认 trackball camera 快捷键。
- 无额外绑定需求。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/KeypressObserver`；需人工按下普通键和方向键确认记录内容及默认样式响应。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_KeypressObserver\result.json。

