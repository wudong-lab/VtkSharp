# StyleSwitch 移植说明

- 官方源码：[StyleSwitch.cxx](https://examples.vtk.org/site/Cxx/Interaction/StyleSwitch/)
- 直接使用现有 `vtkInteractorStyleSwitch`。VTK 内建按键切换 joystick/trackball、camera/actor 模式：`j`/`t` 与 `c`/`a`。
- 无额外绑定需求。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/StyleSwitch`；需人工按 `j`、`t`、`c`、`a` 并验证交互模式切换。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_StyleSwitch\result.json。

