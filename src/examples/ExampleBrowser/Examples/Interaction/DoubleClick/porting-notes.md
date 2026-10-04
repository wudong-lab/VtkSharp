# DoubleClick 移植说明

- 官方源码：[DoubleClick.cxx](https://examples.vtk.org/site/Cxx/Interaction/DoubleClick/)
- 用低优先级左键 observer 重写点击位置差判断，并保留默认相机操作。与原例一样，只按相邻点击位置的 5 像素距离重置计数，不加入时间阈值，因此它展示的是位置判定而非完整桌面双击语义。
- 无额外绑定需求。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/DoubleClick`；需人工测试两次近距离点击、移动超过 5 像素后点击及相机操作。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_DoubleClick\result.json。

