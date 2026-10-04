# SelectVisiblePoints

- 官方源码：[SelectVisiblePoints.cxx](https://examples.vtk.org/site/Cxx/PolyData/SelectVisiblePoints/)
- 按 S 键更新 `vtkSelectVisiblePoints` 并报告当前视角可见的点数。先完成渲染再更新过滤器，使深度缓冲可用。
- 可见性依赖渲染窗口和相机状态；每次视角变化后需重新执行过滤器。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\PolyData_SelectVisiblePoints\result.json。

