# RubberBandPick 移植说明

- 官方源码：[RubberBandPick.cxx](https://examples.vtk.org/site/Cxx/Interaction/RubberBandPick/)
- 使用现有 `vtkInteractorStyleRubberBandPick`。按 `r` 进入橡皮筋框选，再按住鼠标左键拖动；该示例只演示交互样式，不执行数据提取或高亮。
- 无新增绑定。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/RubberBandPick`；需人工按 `r` 后框选。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_RubberBandPick\result.json。

