# RubberBand2D

- 官方源码：[RubberBand2D.cxx](https://examples.vtk.org/site/Cxx/Interaction/RubberBand2D/)
- 使用 `vtkInteractorStyleRubberBand2D` 保留其原生选择框行为；用 interactor 的按下/释放 observer 记录端点，替代 C++ 子类访问的 protected `StartPosition`、`EndPosition`。
- observer 只报告屏幕坐标，不执行数据提取。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_RubberBand2D\result.json。

