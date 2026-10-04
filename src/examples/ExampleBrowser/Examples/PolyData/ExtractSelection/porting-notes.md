# ExtractSelection

- 官方源码：[ExtractSelection.cxx](https://examples.vtk.org/site/Cxx/PolyData/ExtractSelection/)
- 使用 `vtkSelectionSource` 按点 ID 提取 10–19 并叠加显示。避免直接创建 `vtkSelectionNode` 后设置 `vtkInformation` 静态 key 的复杂互操作。
- 官方三视口布局和反选输出未实现；这里保留原始点云与选中点对比。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\PolyData_ExtractSelection\result.json。

