# MouseEvents 移植说明

- 官方源码：[MouseEvents.cxx](https://examples.vtk.org/site/Cxx/Interaction/MouseEvents/)
- 官方版本通过继承 `vtkInteractorStyleTrackballCamera` 并重写三个鼠标按下虚函数，再显式调用基类实现。VtkSharp 不把托管继承映射为 native 虚函数重写，因此改用 interactor 上的低优先级 managed observer，在默认样式处理后记录同类事件。
- 该改写保留默认相机操作和事件报告；不能自定义原生事件处理顺序或拦截事件。
- 无额外绑定需求。验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/MouseEvents`；需人工分别按下三种鼠标键并确认相机交互正常。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_MouseEvents\result.json。

