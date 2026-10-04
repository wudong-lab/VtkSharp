# KeypressEvents 移植说明

- 官方源码：[KeypressEvents.cxx](https://examples.vtk.org/site/Cxx/Interaction/KeypressEvents/)
- 官方源码通过继承交互样式重写 `OnKeyPress()` 并转调基类。改用低优先级 managed observer 检测 `Up` 和 `a`，保留默认相机样式快捷键；这避免依赖 native 虚函数重写。
- 无额外绑定需求。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/KeypressEvents`；需人工按 Up、a 和相机快捷键确认。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_KeypressEvents\result.json。

