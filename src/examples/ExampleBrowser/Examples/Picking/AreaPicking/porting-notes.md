# AreaPicking

- 官方源码：[AreaPicking.cxx](https://examples.vtk.org/site/Cxx/Picking/AreaPicking/)
- 直接调用 `vtkAreaPicker.AreaPick`，在鼠标释放时输出命中 Prop 数量。它保留了区域拾取的穿透语义，但没有逐个高亮命中的 Actor。
- 每次查询都取得借用的 `vtkProp3DCollection` wrapper，仅在回调内读取并释放 wrapper；不释放 picker 所有的集合。

## 验证

- Release 构建通过；截图 smoke 报告：`C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-final-a098c7ac692f4f3485574c29e36bbbc8\result.json`。
- 框选交互仍需在 ExampleBrowser 中人工拖动验收。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Picking_AreaPicking\result.json。

