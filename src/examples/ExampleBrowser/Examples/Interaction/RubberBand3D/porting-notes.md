# RubberBand3D

- 官方源码：[RubberBand3D.cxx](https://examples.vtk.org/site/Cxx/Interaction/RubberBand3D/)
- 使用官方 `vtkInteractorStyleRubberBand3D`，以 interactor 按下/释放 observer 输出矩形端点，替代 C++ 子类访问 protected 位置成员。
- 此示例只演示交互样式，不实现视锥选择或数据提取。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_RubberBand3D\result.json。

