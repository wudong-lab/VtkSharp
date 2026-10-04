# HighlightPickedActor

- 官方源码：[HighlightPickedActor.cxx](https://examples.vtk.org/site/Cxx/Picking/HighlightPickedActor/)
- 以 `vtkPropPicker` observer 代替 C++ 交互器子类；点击命中后将 Actor 设为红色并打开边显示。
- 与官方实现的差异：示例只有一个 Actor，不复制/恢复前一次选择的 property，也不使用随机颜色场景。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Picking_HighlightPickedActor\result.json。

