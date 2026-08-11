# VtkSharp 项目架构

## 项目边界

`VtkSharp` 是 VTK 官方 C++ API 的非官方 .NET 封装。公开仓库负责：

- VTK 官方类型及其 public API 的 C ABI 导出和 C# wrapper。
- 为官方类型手工补充、但生成器暂不适合生成的接口。
- 对象生命周期、事件、字符串和数组等通用互操作基础设施。
- 绑定生成器、白名单、测试和官方 API 示例。

WPF 控件、组织内部派生类型和业务可视化能力不属于公开仓库。

## 运行时分层

```text
.NET application
    ↓
VtkSharp.dll                 # C# wrapper，namespace VtkSharp
    ↓ P/Invoke
VtkSharp.Native.dll          # C ABI shim
    ↓ static link
VTK                          # 一份静态 VTK runtime
```

公开构建不会生成 UI 框架专用程序集。`VtkSharp.Native.dll` 是唯一直接承载 VTK runtime 的 native DLL。

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

## 聚合构建扩展入口

公开 native CMake 提供默认空值的通用源码聚合入口：

```text
VTKSHARP_EXTRA_NATIVE_SOURCES
VTKSHARP_EXTRA_NATIVE_HEADERS
VTKSHARP_EXTRA_INCLUDE_DIRECTORIES
VTKSHARP_EXTRA_VTK_COMPONENTS
VTKSHARP_EXTRA_NATIVE_LIBRARIES
```

这些变量允许外部源码树复用同一个 `VtkSharp.Native` target。额外 VTK 模块会同时进入 `find_package`、`target_link_libraries` 和 `vtk_module_autoinit`，避免模块发现、链接和自动初始化集合不一致。

该入口不改变公开仓库的默认产物，也不允许在公开源码中硬编码具体扩展项目、组织或产品信息。
