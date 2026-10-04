# CallBack

- 官方源码：[CallBack.cxx](https://examples.vtk.org/site/Cxx/Interaction/CallBack/)
- 用 `EndInteractionEvent` managed observer 读取相机位置和焦点。省略官方方向标记 widget 与额外显示管线，只保留回调主题。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_CallBack\result.json。

