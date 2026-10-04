# RubberBand2DObserver

- 官方源码：[RubberBand2DObserver.cxx](https://examples.vtk.org/site/Cxx/Interaction/RubberBand2DObserver/)
- 通过 `vtkInteractorStyleRubberBand2D.SelectionChangedEvent` 观察选择矩形。VTK 回调数据是本次回调期间有效的四个无符号屏幕坐标值；示例立即复制并读取，不保留 native 指针。
- 这是按源码契约读取 callData 的互操作边界；只有事件确实携带四项矩形数据时可复用此解释。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_RubberBand2DObserver\result.json。

