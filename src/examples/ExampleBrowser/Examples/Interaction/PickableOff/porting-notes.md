# PickableOff 移植说明

- 官方源码：[PickableOff.cxx](https://examples.vtk.org/site/Cxx/Interaction/PickableOff/)
- 使用 `vtkInteractorStyleTrackballActor`；右侧圆锥调用 `PickableOff()`，因此不会被选中或拖动，左侧仍可交互。
- 无额外 API 需求；复用本批增加的 `vtkInteractorStyleTrackballActor` 类型。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/PickableOff`；需分别拖动两个圆锥验证差异。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_PickableOff\result.json。

