# TrackballActor 移植说明

- 官方源码：[TrackballActor.cxx](https://examples.vtk.org/site/Cxx/Interaction/TrackballActor/)
- 使用最小新增类型绑定 `vtkInteractorStyleTrackballActor`；球体和圆锥可分别拾取并拖动。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/TrackballActor`；需人工确认拖动 Actor，不应把它与相机旋转混淆。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_TrackballActor\result.json。

