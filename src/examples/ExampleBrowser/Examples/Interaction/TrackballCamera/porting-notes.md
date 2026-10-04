# TrackballCamera 移植说明

- 官方源码：[TrackballCamera.cxx](https://examples.vtk.org/site/Cxx/Interaction/TrackballCamera/)
- 使用现有 `vtkInteractorStyleTrackballCamera` 绑定，显示球体和圆锥以直观区分相机移动与对象运动。
- 无新增绑定。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/TrackballCamera`；需人工拖动旋转、平移和缩放相机。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_TrackballCamera\result.json。

