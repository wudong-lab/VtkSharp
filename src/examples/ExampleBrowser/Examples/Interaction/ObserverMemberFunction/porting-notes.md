# ObserverMemberFunction

- 官方源码：[ObserverMemberFunction.cxx](https://examples.vtk.org/site/Cxx/Interaction/ObserverMemberFunction/)
- 将 C++ 成员回调替换为托管实例方法委托；observer handle 保持回调状态存活到交互结束。

## 验证

- `dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release`：通过（0 warning、0 error）。
- 鼠标键盘交互需在 ExampleBrowser 中人工操作验收。

- 截图 smoke：通过。报告：C:\Users\rhsw\AppData\Local\Temp\vtk-example-smoke-bd3d33af50354db0b682fa1c1a311852\Interaction_ObserverMemberFunction\result.json。

