# RubberBandZoom 移植说明

- 官方源码：[RubberBandZoom.cxx](https://examples.vtk.org/site/Cxx/Interaction/RubberBandZoom/)
- 使用现有 `vtkInteractorStyleRubberBandZoom`，对球体进行橡皮筋框选缩放。
- 无新增绑定。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/RubberBandZoom`；需人工拖动矩形并确认相机缩放到框选区域。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_RubberBandZoom\result.json。

