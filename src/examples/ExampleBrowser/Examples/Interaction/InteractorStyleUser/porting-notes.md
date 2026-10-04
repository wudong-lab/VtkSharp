# InteractorStyleUser 移植说明

- 官方源码：[InteractorStyleUser.cxx](https://examples.vtk.org/site/Cxx/Interaction/InteractorStyleUser/)
- 使用 `vtkInteractorStyleUser`，配合 `vtkRenderWindowInteractor` 上的托管左键 observer。User style 不提供默认相机或 Actor 操作；点击只触发回调。
- 增加 `vtkInteractorStyleUser` 类型绑定，不增加额外方法。managed `AddObserver` 替代原例的 `vtkCallbackCommand`。
- 验证：待运行 `tools/verify-workflow.ps1 -VtkDir <VTK_DIR> -Example Interaction/InteractorStyleUser`；需人工确认点击触发日志且拖动不会产生默认相机行为。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-rerun-ae6c6fc86f114301a3702bcc002ab13f\Interaction_InteractorStyleUser\result.json。

