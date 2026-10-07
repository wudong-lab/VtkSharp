# VtkSharp 项目架构

当前默认采用单一 native 导出层与静态 VTK，构建和部署见 [构建说明](build/vtksharp.md)。

## 项目边界

`VtkSharp` 是 VTK 官方 C++ API 的非官方 .NET 封装，项目包含：

- VTK 官方类型及其 public API 的 C ABI 导出和 C# wrapper。
- 为官方类型手工补充、但生成器暂不适合生成的接口。
- 对象生命周期、事件、字符串和数组等通用互操作基础设施。
- 绑定生成器、白名单、测试和官方 API 示例。

## 运行时分层

```text
.NET application
    ↓
VtkSharp.dll                 # C# wrapper，namespace VtkSharp
    ↓ P/Invoke
VtkSharp.Native.dll         # 统一 C ABI shim，内部静态链接 VTK
```

全部公开导出归属同一 native target。私有产品通过源码聚合接口把公开、私有及 WPF native 实现共同链接到该目标，进程内只使用这一份 VTK 实现。

NuGet native 资产通过 `buildTransitive/VtkSharp.targets` 复制到应用输出目录，与 `VtkSharp.dll` 同级。P/Invoke 按 .NET/Windows 常规规则从应用目录解析 native DLL，不使用自定义加载器或运行目录配置。静态 VTK 与 wrapper 使用兼容工具链和 `/MD`（Debug `/MDd`）。

## 源码布局

```text
src/bindings/VtkSharp/            # managed wrapper 与手写辅助代码
src/bindings/VtkSharp.Native/     # C ABI 导出与 native CMake 项目
src/bindings/VtkSharp.Tests/      # 绑定层测试
src/generator/                    # 生成器、配置、schema 和白名单
src/examples/ExampleBrowser/      # 官方 API 示例浏览器
```

生成文件和手写文件可以位于同一模块目录，但生成文件使用明确的生成标记和文件名约定。重新生成绑定不得覆盖手写 partial、runtime helper 或手工 C ABI 导出。

## 对象生命周期

native 对象所有权以 VTK 引用计数语义为准：

- wrapper 创建并拥有的对象负责释放自身持有的引用。
- native 侧长期保存的指针、回调或委托必须有明确的保活和解绑顺序。
- 指针、字符串、数组和结构体跨边界传递时必须明确内存归属、编码与布局。
- 不通过异常捕获掩盖所有权或 ABI 不确定性，应以最小生命周期测试验证。

从外部 native 指针创建 wrapper 时，使用 `FromBorrowedPointer` 或 `TakeReference`，具体所有权约定见 [Native 指针封装与所有权](native-pointer-ownership.md)。
