# SelectAnActor 移植说明

- 官方源码：[SelectAnActor.cxx](https://examples.vtk.org/site/Cxx/Interaction/SelectAnActor/)
- 官方派生样式读取 protected 成员 `InteractionProp` 判断当前拖动对象。改用现有 `vtkPropPicker` 在 interactor observer 中按 native 对象指针身份比较 cube/sphere，同时保留 `vtkInteractorStyleTrackballActor` 的拖动行为。
- 对简单 Actor 场景，该判断与原示例目标一致；没有实现 assembly 路径上的复合对象判断。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/SelectAnActor`；需人工点击并拖动立方体、球体及空白处。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_SelectAnActor\result.json。

