# UserEvent

- 官方源码：[UserEvent.cxx](https://examples.vtk.org/site/Cxx/Interaction/UserEvent/)
- 保留自定义事件注册、触发和观察行为；用 U 键触发 `vtkCommand.UserEvent + 1`，省略官方自定义 native filter。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_UserEvent\result.json。

