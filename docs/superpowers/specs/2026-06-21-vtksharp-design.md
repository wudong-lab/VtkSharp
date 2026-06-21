# VtkSharp 初始技术方案基线

本文记录 VtkSharp 项目在 2026-06-21 方案讨论中已经确认的设计决策，作为后续架构、实现计划和代码评审的共同基础。

## 项目定位

VtkSharp 是一个非官方的开源 VTK .NET 绑定项目，目标是为工程软件、CAD/CAE 可视化和 Windows 桌面软件提供可控、可裁剪、可维护的 VTK 访问能力。

项目首要目标是满足自身工程项目对开源三维图形能力的需求，同时作为大型 C++ 项目 .NET 封装技术和 AI 辅助编程方法的实践样板。

第一阶段不追求 VTK API 全量覆盖，不以复刻 ActiViz 为目标，也不依赖 Kitware 官方商业 .NET 封装。

## 开源与商用

项目采用 BSD-3-Clause 许可证，与 VTK 的宽松许可风格保持一致。项目允许在商业闭源软件中使用，但发布商业产品时需要保留 VtkSharp、VTK 以及相关第三方依赖的版权声明、许可证文本和免责声明。

项目文档和 README 应明确说明：

```text
VtkSharp is an unofficial open-source .NET binding for VTK.
It is not affiliated with or endorsed by Kitware.
```

后续需要补充：

```text
NOTICE.md
THIRD-PARTY-NOTICES.md
```

## 总体架构

VtkSharp 采用 C ABI shim 加 C# P/Invoke 的封装路线。

基本结构如下：

```text
VTK C++ API
    |
    v
VtkSharp native C ABI shim
    |
    v
VtkSharp managed thin wrapper
    |
    +--> optional WPF integration
    +--> future Avalonia integration
```

C# 层不直接绑定 C++ 类 ABI，不直接调用 C++ 成员函数。跨语言边界统一通过 `extern "C"` 导出的 C ABI 函数完成。

示例：

```cpp
VTKSHARP_API vtkSphereSource* vtkSphereSource_New();
VTKSHARP_API void vtkSphereSource_SetRadius(vtkSphereSource* self, double value);
```

对应 C# 第一阶段使用：

```csharp
[DllImport("vtksharp_native")]
private static extern IntPtr vtkSphereSource_New();
```

## VTK 链接方式

第一阶段采用动态链接 VTK：

```text
C# managed assembly
    -> vtksharp_native.dll
        -> VTK runtime DLLs
```

选择动态链接的原因：

- 构建复杂度低；
- 更适合开源项目起步；
- 便于调试、替换和升级 VTK；
- NuGet runtime assets 分发边界更清晰；
- 第三方许可声明更直观。

静态链接 VTK 作为后续高级构建和商业产品分发选项保留，不作为第一阶段目标。

## Native 构建系统

Native shim 第一阶段即采用 CMake，而不是 Visual Studio `.vcxproj` 作为主构建系统。

原因：

- VTK 本身是 CMake 生态；
- `find_package(VTK CONFIG REQUIRED)` 是集成 VTK 的自然方式；
- 后续可预留 Linux、macOS 构建可能性；
- CI 和跨平台 native package 扩展更顺畅。

第一阶段只承诺 Windows x64，但工程结构应避免锁死到 Windows/MSVC。

推荐 native 目录形态：

```text
native/
  CMakeLists.txt
  CMakePresets.json
  include/
    vtksharp_api.h
  src/
    manual/
    generated/
```

跨平台导出宏采用统一头文件定义：

```cpp
#if defined(_WIN32)
#define VTKSHARP_API extern "C" __declspec(dllexport)
#else
#define VTKSHARP_API extern "C" __attribute__((visibility("default")))
#endif
```

## 目标框架

核心托管绑定层第一阶段面向 `netstandard2.0`。

原因：

- 兼容 .NET Framework 4.8；
- 适合 AutoCAD、Revit、传统 WPF 工程软件和插件生态；
- 降低第一阶段实现复杂度；
- 对 VTK thin wrapper 来说，最新版 .NET 特性不是必要前提。

第一阶段核心层采用：

```text
TargetFramework = netstandard2.0
InteropBackend = DllImport
```

同时预留后续迁移到 multi-target 的基础设施：

```text
TargetFrameworks = netstandard2.0;net9.0
InteropBackend = DllImport;LibraryImport
```

生成器内部应将目标框架和 interop 后端建模为显式选项，不把 `netstandard2.0` 和 `DllImport` 写死到绑定模型中。

## UI 分层

核心库不依赖任何 UI 框架。

第一阶段 UI 集成优先支持 WPF，后续预留 Avalonia 支持。

推荐分层：

```text
VtkSharp
  netstandard2.0
  VTK 对象绑定、pipeline、rendering/data model API

VtkSharp.Wpf
  net48 first
  future net9.0-windows
  WPF HwndHost / D3DImage / Windows rendering host

VtkSharp.Avalonia
  future package
  Avalonia rendering host
```

WPF、Avalonia、WinForms 等具体 UI 类型不得出现在核心库公共 API 中。

第一阶段不为 Avalonia 过度抽象 UI 宿主，只保证核心层 UI 无关、native 层 CMake 化、WPF 层独立包。

## AutoCAD 与旧版插件兼容

AutoCAD 老插件常见运行环境是 .NET Framework 4.8。核心层选择 `netstandard2.0` 后，AutoCAD 插件可以直接引用 VtkSharp 核心绑定层。

如果 VTK 窗口运行在 AutoCAD 进程内，即使是独立顶层窗口，也仍需要可被 .NET Framework 4.8 加载的托管程序集。因此 WPF 包第一阶段应考虑 `net48`。

如果未来需要现代 .NET Viewer 独立进程，可采用文件交换、Named Pipe、MemoryMappedFile 等 IPC 方案，但这不是第一阶段核心绑定项目的必要前提。

## API 风格

第一阶段采用 thin wrapper 路线。

公共 API 尽可能保持与 VTK C++ API 一致：

- 类名保留 `vtk` 前缀；
- 方法名尽量与 C++ API 一致；
- 用户可通过阅读 VTK 官方 C++ 示例直接编写 VtkSharp 代码；
- 便于 AI 将 VTK C++ 示例转换为 C# 版本。

示例：

```csharp
using var sphere = vtkSphereSource.New();
sphere.SetRadius(10);
sphere.SetCenter(0, 0, 0);

using var mapper = vtkPolyDataMapper.New();
mapper.SetInputConnection(sphere.GetOutputPort());

using var actor = vtkActor.New();
actor.SetMapper(mapper);
```

除 C++ API 直接对应接口外，可以通过 `partial class` 添加少量 C# 便利重载，但不能遮蔽、替代或改变 VTK 原始语义。

示例：

```csharp
public partial class vtkSphereSource
{
    public void SetCenter(VtkSharpPoint3 center)
    {
        SetCenter(center.X, center.Y, center.Z);
    }
}
```

## 补充类型命名规则

为避免与 VTK 原生类型混淆，命名规则固定为：

```text
vtk*       -> VTK 原生绑定类型
VtkSharp* -> VtkSharp 自定义补充类型
```

示例：

```text
VTK 原生绑定类型：
  vtkRenderer
  vtkActor
  vtkPolyData
  vtkPoints
  vtkMatrix4x4
  vtkTransform

VtkSharp 补充类型：
  VtkSharpPoint3
  VtkSharpVector3
  VtkSharpColor3
  VtkSharpColor4
  VtkSharpBounds3
```

规则：

- 不定义裸名 `Point3D`、`Vector3D`、`Color`、`Bounds`；
- 不使用容易误认为 VTK 类型的 `VtkPoint3D`；
- 默认数值精度使用 `double`；
- 如后续需要 `float`，使用显式后缀，例如 `VtkSharpPoint3F`。

## 对象生命周期与所有权

VtkSharp 的生命周期模型应忠实表达 VTK 引用计数语义，同时使用 C# 的 `IDisposable` 和 `using` 模式。

核心概念：

```text
owned wrapper:
  当前 C# wrapper 拥有一个 VTK reference
  Dispose 会释放该 reference

borrowed wrapper:
  当前 C# wrapper 只观察 native 指针
  Dispose 不释放 native 对象

retained wrapper:
  从 borrowed wrapper 显式 Register 得到的 owned wrapper
  Dispose 会释放该 reference
```

基础规则：

```text
New()        -> owned wrapper
GetXxx()     -> borrowed wrapper，除非 manifest 显式覆盖
Register()  -> 创建 owned wrapper
Dispose()   -> 释放当前 wrapper 持有的 VTK reference
```

第一阶段实现策略：

- 所有 `vtkObjectBase` 派生 wrapper 实现 `IDisposable`；
- 不使用 finalizer；
- 不使用 `SafeHandle`；
- 使用 `IntPtr NativePointer` 和 `bool OwnsReference` 表达当前 wrapper 是否拥有引用；
- 文档要求用户对 owned wrapper 使用 `using` 或显式 `Dispose()`。

原因是 VTK 对象可能关联 OpenGL context、UI 线程和特定销毁顺序。由 finalizer 线程异步释放 VTK 对象存在潜在风险。

## 返回值所有权与 marshal 规则

生成器采用默认规则加 manifest 显式覆盖。

默认规则：

```text
New() / NewInstance() / CreateInstance()
  默认返回 owned

GetXxx()
  默认返回 borrowed

MakeXxx() / CreateXxx()
  不自动猜测，必须在 manifest 中标注

primitive / enum
  无所有权

char* / const char*
  默认复制为 string

primitive*
  必须在 manifest 中提供 marshal rule，否则跳过或报错
```

示例 manifest：

```yaml
classes:
  vtkRenderer:
    methods:
      - name: GetActiveCamera
        returns:
          ownership: borrowed
          nullable: false

      - name: MakeLight
        returns:
          ownership: owned
          nullable: false
```

对于 `double* GetPosition()`、`double* GetBounds()` 一类返回内部数组指针的接口，第一阶段不直接暴露 native 指针。应在 manifest 中显式声明固定数组长度和托管类型。

示例：

```yaml
- name: GetPosition
  returns:
    marshal: fixedArray
    elementType: double
    length: 3
    managedType: VtkSharpPoint3

- name: GetBounds
  returns:
    marshal: fixedArray
    elementType: double
    length: 6
    managedType: VtkSharpBounds3
```

优先生成 native copy shim，而不是把内部数组指针交给 C# 用户持有：

```cpp
VTKSHARP_API void vtkCamera_GetPosition(vtkCamera* self, double* values)
{
    auto p = self->GetPosition();
    values[0] = p[0];
    values[1] = p[1];
    values[2] = p[2];
}
```

## Manifest 与生成器

API 覆盖范围由 manifest 控制，不从生成代码反推。

manifest 是项目 API 覆盖的事实来源，用于记录需要绑定的 VTK 类、方法、所有权规则、marshal 规则和必要的 nullable 标注。

推荐目录：

```text
bindings/
  vtk-9.6/
    core.yaml
    rendering.yaml
    data-model.yaml
    wpf.yaml
```

示例：

```yaml
vtkVersion: "9.6"
module: RenderingCore

classes:
  vtkRenderer:
    methods:
      - AddActor
      - RemoveActor
      - ResetCamera
      - SetBackground
      - name: GetActiveCamera
        returns:
          ownership: borrowed
          nullable: false
```

生成器内部建议分为两层：

```text
BindingModel:
  VtkClass
  VtkMethod
  VtkType
  VtkParameter
  OwnershipRule
  MarshalRule

EmitOptions:
  TargetFramework
  InteropBackend
  NativeLibraryName
  UseUnsafe
```

第一阶段只实现：

```text
TargetFramework = netstandard2.0
InteropBackend = DllImport
```

但架构上预留：

```text
TargetFramework = net9.0 / net10.0
InteropBackend = LibraryImport
Span / ReadOnlySpan 便利重载
NativeLibrary resolver
```

## AI 辅助生成策略

AI 可以参与 manifest 编写，尤其适合分析固定长度数组返回值、常见所有权模式和 VTK 示例转换。

但 AI 只能作为辅助建议来源，不能静默决定内存语义和所有权语义。

推荐流程：

```text
1. 解析器发现需要人工决策的 API
2. 生成器输出诊断
3. AI 根据头文件、文档和同类 API 给出 manifest 建议
4. 规则校验器验证 manifest 合法性
5. 人工 review 后合并
```

对于没有 manifest marshal rule 的 `primitive*` 返回值，生成器应报错或跳过，不生成危险的 public pointer API。

## 生成代码管理

第一阶段建议提交生成代码。

原因：

- 使用者 clone 后不必先运行生成器；
- PR diff 可以看到真实 API 变化；
- NuGet 打包更简单；
- 生成器演进期降低构建门槛；
- 对开源贡献者更友好。

CI 后续应增加校验：

```text
run generator
git diff --exit-code
```

确保 manifest、生成器和生成代码一致。

## 推荐仓库结构

当前仓库仍处于初始状态。后续推荐逐步演进为：

```text
VtkSharp/
  README.md
  LICENSE
  NOTICE.md
  THIRD-PARTY-NOTICES.md
  Directory.Build.props
  VtkSharp.slnx

  src/
    VtkSharp/
    VtkSharp.Wpf/

  native/
    CMakeLists.txt
    CMakePresets.json
    include/
    src/
      manual/
      generated/

  generator/
    VtkSharp.Generator.Core/
    VtkSharp.Generator.Cli/

  bindings/
    vtk-9.6/

  tests/
    VtkSharp.Tests/
    VtkSharp.NativeSmokeTests/
    VtkSharp.Wpf.Tests/

  samples/
    Console/
    Wpf/

  docs/
    design/
    binding-rules/
    native-build/
    packaging/
```

## 后续待讨论主题

以下内容尚未完整定稿，后续需要继续讨论：

- 第一阶段 MVP API 覆盖清单；
- manifest schema 的精确定义；
- CMake 与 .NET build 的编排方式；
- native DLL 与 VTK runtime DLL 的 NuGet 分发策略；
- WPF render host 的具体实现路线；
- 单元测试、native smoke test 和 UI smoke test 策略；
- VTK 版本选择和升级策略；
- 示例转换工具是否作为正式项目组件；
- CI 构建矩阵；
- 文档站点和示例组织方式。
