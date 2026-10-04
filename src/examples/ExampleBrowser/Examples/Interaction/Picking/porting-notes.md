# Picking 移植说明

- 官方源码：[Picking.cxx](https://examples.vtk.org/site/Cxx/Interaction/Picking/)
- C++ 通过派生 trackball camera style 在左键事件中 pick；改为 interactor 的托管 observer，命中时读取 Actor 与世界坐标，并在命中位置添加小球标记。
- observer 位于默认样式之后，因此未命中时仍会执行默认 trackball camera 处理。官方子类只在命中 Actor 时才显式调用基类处理，这是本移植的交互差异。
- 新增 `vtkAbstractPicker.GetPickPosition(double[3])` 绑定，用托管 `Span<double>` 接收固定长度 3 的坐标，避免暴露 native 返回指针；`vtkPropPicker.GetActor()` 返回借用 wrapper，仅在检查期间使用。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/Picking`；需人工点击平面，确认坐标及标记位置，并测试空白处。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_Picking\result.json。

