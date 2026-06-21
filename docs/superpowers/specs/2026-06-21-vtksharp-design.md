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
    +--> future WPF integration
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

## VTK 模块化与裁剪

完整 VTK 体量很大，但实际工程项目通常只需要其中部分模块。VTK 自身的 DLL 和 CMake target 也是模块化的，因此 VtkSharp 从 MVP 开始就应具备模块意识，但不在 MVP 中拆分产物。

策略：

```text
模块意识前置
模块拆分后置
```

MVP 必须做到：

```text
TypeGraph 记录 class -> VTK module；
manifest 记录 module；
Resolved Binding Model 记录模块依赖；
CMake COMPONENTS 由 profile / manifest 推导；
native 只 link 实际需要的 VTK modules；
引入 profile 表达 API 裁剪范围。
```

MVP 暂不做：

```text
不拆多个 managed assemblies；
不拆多个 native DLL；
不拆多个 NuGet packages；
不做复杂 VTK runtime dependency pruning。
```

第一阶段产物仍保持：

```text
VtkSharp.dll
vtksharp_native.dll
```

但 native CMake 应只链接当前 profile 需要的 VTK module targets。例如第一批 `mvp-data` profile：

```cmake
find_package(VTK CONFIG REQUIRED COMPONENTS
  CommonCore
  CommonDataModel
)

target_link_libraries(vtksharp_native
  PRIVATE
    VTK::CommonCore
    VTK::CommonDataModel
)
```

后续可以在不推翻 manifest 和生成器模型的前提下演进为：

```text
单 managed/native 产物 + 不同 profile 裁剪
或
VtkSharp.CommonCore / VtkSharp.CommonDataModel / VtkSharp.RenderingCore 等分包
```

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

## 第一阶段 MVP 范围

第一阶段 MVP 暂不考虑 WPF、Avalonia、WinForms 或其他 UI 宿主集成。MVP 的目标是先证明核心绑定层、native C ABI shim、工程几何数据构造、对象生命周期和最小 VTK data model/pipeline 能稳定工作。

MVP 重点不是运行一个窗口示例，而是让 C# 工程软件能够把自己的点、线、面数据可靠写入 VTK 数据结构，并能通过 smoke test 验证数据正确性。

MVP done 条件：

```text
C# 通过 VtkSharp 创建 vtkPoints / vtkCellArray / vtkPolyData
能检查 point count / cell count / bounds
能正确表达 owned / borrowed wrapper 和 Dispose 语义
不要求 WPF
不要求窗口渲染
不要求截图像素验证
```

MVP API 覆盖分三层。

第一层是对象和数据模型，必须优先覆盖：

```text
vtkObjectBase
vtkObject
vtkAlgorithm
vtkAlgorithmOutput
vtkDataObject
vtkDataSet
vtkPointSet
vtkPolyData
vtkPoints
vtkCellArray
```

第二层是最小 pipeline，范围收紧，只用于验证数据能够接入 VTK pipeline：

```text
vtkPolyDataMapper
vtkActor
vtkProperty
vtkRenderer
vtkRenderWindow
vtkCamera
```

`vtkRenderWindowInteractor` 不进入 MVP，避免窗口消息循环、宿主程序线程模型和 UI 集成问题过早进入核心绑定阶段。

第三层是示例数据源，可选但建议少量覆盖，用于 smoke test 和示例：

```text
vtkSphereSource
vtkCubeSource
vtkLineSource
```

MVP 的核心示例应以手工构造 `vtkPolyData` 为主，而不是以 source 类 demo 为中心。VtkSharp 的第一阶段价值是让工程软件中的自有几何数据进入 VTK，而不是只复现一个 sphere 示例。

第一批实际实现进一步压缩为 triangle `vtkPolyData` native smoke test，不包含 rendering pipeline。

第一批类型：

```text
vtkObjectBase
vtkObject
vtkDataObject
vtkDataSet
vtkPointSet
vtkPolyData
vtkAbstractCellArray
vtkCellArray
vtkPoints
VtkSharpBounds3
```

第一批方法：

```text
vtkObjectBase:
  Register
  UnRegister
  GetReferenceCount

vtkObject:
  GetMTime
  Modified

vtkPoints:
  New
  GetNumberOfPoints
  FromArray(double[] xyz)

vtkCellArray:
  New
  GetNumberOfCells
  FromTriangles(long[] ids)
  FromTriangles(int[] ids)

vtkPolyData:
  New
  SetPoints(vtkPoints points)
  SetPolys(vtkCellArray polys)
  GetNumberOfPoints
  GetNumberOfCells
  GetBounds() -> VtkSharpBounds3
```

第一批暂不包含：

```text
vtkAlgorithm
vtkPolyDataMapper
vtkActor
vtkRenderer
vtkRenderWindow
vtkCamera
lines / polyline API
WPF / UI
rendering smoke test
```

## 示例驱动 API 覆盖

VTK API 规模非常庞大，VtkSharp 不以全量导出为目标。API 覆盖应由官方示例和实际工程场景驱动，逐步添加应用层真正需要的类型和方法。

基本原则：

```text
不扫描并导出完整 VTK public API；
优先翻译 VTK 官方示例和项目工程场景；
示例中显式使用的类型和方法作为直接 API 需求；
TypeGraph 自动补齐隐式基类；
生命周期、marshal、ownership、overload 不明确的 API 进入 decisions；
示例或 smoke test 通过后再扩大 API 覆盖。
```

API 覆盖来源分两类：

```text
official example:
  VTK 官方示例，适合验证 API 风格、pipeline 和常见用法

engineering scenario:
  VtkSharp 面向工程软件/CAD/CAE 的实际场景，适合补足官方示例覆盖不到的数据构造、mesh、polyline、属性和互操作需求
```

每个示例扩展应产生可审查的 coverage delta：

```text
new classes
new methods
new value types
new custom shims
new decisions
new tests / samples
```

推荐阶段：

```text
Stage 1 data:
  Triangle
  PolyLine
  manual PolyData

Stage 2 basic pipeline:
  Sphere
  Cube
  Actor / Mapper / Renderer

Stage 3 data attributes:
  Scalars
  Colors
  Normals

Stage 4 filters:
  CleanPolyData
  TriangleFilter
  Cutter / Clip

Stage 5 IO:
  STL / OBJ / VTP reader writer

Stage 6 interaction/UI:
  RenderWindowInteractor
  WPF host
```

## UI 分层

核心库不依赖任何 UI 框架。

第一阶段 MVP 暂不实现 UI 集成。WPF 是后续优先考虑的桌面 UI 集成方向，Avalonia 作为更远期的跨平台 UI 集成方向预留。

长期推荐分层：

```text
VtkSharp
  netstandard2.0
  VTK 对象绑定、pipeline、rendering/data model API

VtkSharp.Wpf
  future net48 first
  future net9.0-windows
  WPF HwndHost / D3DImage / Windows rendering host

VtkSharp.Avalonia
  future package
  Avalonia rendering host
```

WPF、Avalonia、WinForms 等具体 UI 类型不得出现在核心库公共 API 中。

MVP 只保证核心层 UI 无关、native 层 CMake 化。WPF 层独立包和 Avalonia 层均不作为 MVP 交付项。

## AutoCAD 与旧版插件兼容

AutoCAD 老插件常见运行环境是 .NET Framework 4.8。核心层选择 `netstandard2.0` 后，AutoCAD 插件可以直接引用 VtkSharp 核心绑定层。

如果未来需要让 VTK 窗口运行在 AutoCAD 进程内，即使是独立顶层窗口，也仍需要可被 .NET Framework 4.8 加载的托管程序集。因此后续 WPF 包应优先考虑 `net48`。

如果未来需要现代 .NET Viewer 独立进程，可采用文件交换、Named Pipe、MemoryMappedFile 等 IPC 方案，但这不是 MVP 核心绑定项目的必要前提。

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

## 工程几何数据输入

MVP 对工程几何数据构造采用“VTK 原始 API + 少量 C# 便利 API”的策略。

公共 API 原则：

```text
1. 保留 VTK 原始语义和命名；
2. 对工程几何数据构造补少量 C# 便利 API；
3. 便利 API 只做数据搬运和形状表达，不隐藏 VTK pipeline 语义。
```

`vtkCellArray` 应保留贴近 VTK 的逐步构造方式：

```csharp
cells.InsertNextCell(3);
cells.InsertCellPoint(0);
cells.InsertCellPoint(1);
cells.InsertCellPoint(2);
```

同时可通过 `partial class` 提供少量常用便利方法：

```csharp
cells.InsertTriangle(0, 1, 2);
cells.InsertQuad(0, 1, 2, 3);
cells.InsertPolyline(pointIds);
```

批量构造 API 第一阶段保持克制，优先服务工程软件中常见的点、线段、三角面和四边形面：

```csharp
using var points = vtkPoints.FromArray(xyz);
using var polys = vtkCellArray.FromTriangles(triangles);
using var lines = vtkCellArray.FromLines(lineSegments);

using var polyData = vtkPolyData.New();
polyData.SetPoints(points);
polyData.SetPolys(polys);
polyData.SetLines(lines);
```

MVP 用户侧便利 API 采用简单扁平数组表达：

```text
points:    [x, y, z, x, y, z, ...]
triangles: [p0, p1, p2, p0, p1, p2, ...]
lines:     [p0, p1, p0, p1, ...]
```

底层设计应保留 VTK 9 `vtkCellArray` 的 offsets + connectivity 模型扩展口，后续支持 polygon、polyline、mixed cells 时不推翻设计。但 MVP 不要求把 offsets + connectivity 作为主要用户入口。

MVP 所有托管数组输入均采用 copy semantics：

```text
调用返回后，用户可以安全修改或释放原始 C# 数组；
VtkSharp 不持有托管数组引用；
VtkSharp 不在 MVP 中暴露零拷贝或外部内存引用 API。
```

零拷贝路径后续可作为显式高级 API 单独设计，例如命名中明确包含 `External` 或 `Unsafe`，并独立处理 pin、内存归属、VTK array ownership 和线程模型问题。

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

## vtkIdType 映射

VTK 的 point id、cell id 和通用 id 语义应映射到 `vtkIdType`。MVP 中 C# public API 使用 `long` 表达 VTK id：

```text
native: vtkIdType
C ABI: int64_t 或项目内明确 typedef
C#: long
```

便利 API 主要接受 `long[]`，可额外提供 `int[]` 重载以方便小规模示例和常见工程数据输入，内部统一转换到 `long` / `vtkIdType`。

C ABI 不应让 C# 侧猜测 `vtkIdType` 的大小。第一阶段 Windows x64 + 常规 VTK 构建下可使用构建期校验：

```cpp
static_assert(sizeof(vtkIdType) == sizeof(int64_t));
```

如果未来支持特殊 VTK 配置或非 64-bit `vtkIdType`，需要在 native shim 层增加适配，而不是改变 C# public API 的 id 语义。

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

`Dispose()` 行为按 wrapper 自身 ownership 决定：

```text
owned wrapper Dispose:
  释放当前 wrapper 持有的 VTK reference
  标记当前 wrapper disposed

borrowed wrapper Dispose:
  不释放 native object
  只标记当前 wrapper disposed

Dispose 后调用 public 方法:
  抛 ObjectDisposedException
```

实现上 wrapper 应有显式 disposed 状态，不只依赖 `NativePointer == IntPtr.Zero` 推断。

对象输入参数默认按 borrowed input 处理：

```text
SetXxx(vtkObject*) / AddXxx(vtkObject*) / InsertXxx(vtkObject*)
  不转移 C# wrapper ownership
  调用后参数 wrapper 的 OwnsReference 不变
  VTK 内部是否 Register 由 VTK API 自己负责
```

manifest 可为未来保留 `ownership: transfer` 概念，但 MVP 不支持对象参数所有权转移。

MVP 不做 wrapper identity cache。同一个 native pointer 可以对应多个 C# wrapper 实例，每个 wrapper 只管理自身的 owned reference 或 borrowed 状态。

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

对于 `double* GetBounds()`、`void GetBounds(double bounds[6])` 这类固定长度数组或内部指针 API，public API 应使用语义化 `VtkSharp*` 值类型；raw P/Invoke 只作为 internal 细节存在。

推荐结构：

```text
C++ 原始危险 API:
  不直接导出为 public C# API

native copy shim:
  exported C ABI
  internal P/Invoke

C# 语义 API:
  public
  使用 VtkSharpBounds3 / VtkSharpPoint3 / VtkSharpColor3 等值类型
```

示例：

```cpp
VTKSHARP_API void vtkDataSet_GetBounds_copy(vtkDataSet* self, double* bounds)
{
    self->GetBounds(bounds);
}
```

```csharp
public VtkSharpBounds3 GetBounds()
{
    var values = new double[6];
    NativeMethods.vtkDataSet_GetBounds_copy(NativePointer, values);
    return VtkSharpBounds3.FromArray(values);
}
```

除了 `vtkObjectBase` 派生的引用计数对象外，VTK API 中还会出现非引用计数类型，例如 struct、enum、typedef、模板值类型和轻量 helper。生成器模型应区分：

```text
VtkObjectType:
  vtkObjectBase 派生
  引用计数
  C# wrapper class + IDisposable

VtkValueType:
  非引用计数
  by-value / by-ref / fixed-array marshal
  C# readonly struct 或 VtkSharp 补充类型

VtkEnumType:
  enum 映射

UnsupportedNativeType:
  需要 manifest 决策，默认跳过或报诊断
```

MVP 不大规模导出 VTK 原生非引用计数类型。优先把稳定、简单、值语义明确的概念映射为 `VtkSharp*` 补充类型，例如 `VtkSharpBounds3`、`VtkSharpPoint3`、`VtkSharpVector3`、`VtkSharpColor3`、`VtkSharpColor4`。复杂模板、iterator、range、`std::vector`、`std::map`、`std::function`、`vtkSmartPointer<T>` 和内部 detail 类型默认不导出。

## Manifest 与生成器

API 覆盖范围由 manifest 控制，不从生成代码反推。

manifest 是项目 API 覆盖的事实来源，用于记录需要绑定的 VTK 类、方法、所有权规则、marshal 规则和必要的 nullable 标注。

生成器输入模型分三层：

```text
Hierarchy Model:
  来自 VTK hierarchy 文件
  解决类、基类、header、module、WRAPEXCLUDE、模板跳过

API Manifest Model:
  来自 bindings/*.yaml
  解决哪些类/方法导出、marshal、ownership、visibility、overloadId

Resolved Binding Model:
  由生成器合并前两者得到
  解决实际生成哪些 C# 类型、native shim、public/internal 方法和 VTK module dependencies
```

推荐目录：

```text
bindings/
  vtk-9.6/
    profiles.yaml
    examples/
      official.yaml
      engineering.yaml
    generated/
      CommonCore.generated.yaml
      CommonDataModel.generated.yaml
    overrides/
      CommonCore.overrides.yaml
      CommonDataModel.overrides.yaml
    decisions/
      CommonDataModel.decisions.yaml
    resolved/
      mvp-data.resolved.yaml
```

`bindings/vtk-9.6/` 是 manifest 基线目录。CMake 构建不在 MVP 中硬编码 VTK patch 版本，采用 `find_package(VTK CONFIG REQUIRED)` 查找本地 VTK；但代码和 smoke test 以 VTK 9.6 开发环境验证。其他 VTK 9.x 暂视为 best effort，不作为 MVP 兼容性承诺。

Manifest 分层规则：

```text
generated:
  由规则、工具或 AI 自动生成
  可重复生成
  不人工编辑
  可被 overrides 覆盖

overrides:
  人工确认或人工修正
  优先级高于 generated
  PR review 重点关注

decisions:
  记录 AI 或规则无法确定的问题、候选项、证据和人工选择
  status != accepted 的条目不参与最终代码生成

resolved:
  generated + overrides + accepted decisions 的合并结果
  真正用于代码生成
```

生成器 emit 阶段只接受 resolved manifest。未确认的 AI 建议不能直接进入 public API。

示例 manifest 用于记录 API 覆盖来源：

```yaml
examples:
  Triangle:
    source: https://examples.vtk.org/site/Cxx/GeometricObjects/Triangle/
    category: data-model
    priority: mvp
    expectedApis:
      classes:
        - vtkPoints
        - vtkCellArray
        - vtkPolyData
      methods:
        - vtkPoints.InsertNextPoint
        - vtkCellArray.InsertNextCell
        - vtkPolyData.SetPoints
        - vtkPolyData.SetPolys
```

manifest 条目应可记录 coverage source，便于 code review 和后续裁剪：

```yaml
classes:
  vtkPolyData:
    metadata:
      coverageSources:
        - examples/official/GeometricObjects/Triangle
    methods:
      - name: SetPoints
        metadata:
          coverageSources:
            - examples/official/GeometricObjects/Triangle
```

如果某个 API 没有示例或工程场景来源，review 时应要求说明为什么需要现在导出。

Manifest 应支持 profile 概念，用于表达裁剪范围、生成输入集合、CMake VTK components 和测试范围。

第一批 profile：

```yaml
profiles:
  mvp-data:
    modules:
      - CommonCore
      - CommonDataModel
    classes:
      - vtkObjectBase
      - vtkObject
      - vtkDataObject
      - vtkDataSet
      - vtkPointSet
      - vtkPolyData
      - vtkAbstractCellArray
      - vtkCellArray
      - vtkPoints
```

后续 rendering profile 示例：

```yaml
profiles:
  rendering-basic:
    modules:
      - CommonCore
      - CommonDataModel
      - CommonExecutionModel
      - RenderingCore
      - RenderingOpenGL2
```

生成器应校验 profile 中声明的 module 与 hierarchy/manifest 中解析出的 class module 一致。CMake 的 VTK `COMPONENTS` 应由选定 profile 的 resolved module dependencies 推导，而不是手写完整 VTK 依赖集合。

TypeGraph 主来源采用 VTK 安装目录下的 hierarchy 文件：

```text
<VTK_INSTALL>/lib/vtk-<version>/hierarchy/VTK/*-hierarchy.txt
```

该目录路径不应写死，生成器通过配置或命令行参数接收：

```text
VtkHierarchyDir
```

hierarchy 解析规则：

```text
class line:
  vtkPolyData : vtkPointSet ; vtkPolyData.h ; vtkCommonDataModel

root class line:
  vtkObjectBase ; vtkObjectBase.h ; vtkCommonCore

ignore:
  WRAPEXCLUDE
  nested symbols with ::
  template symbols with <...>
  typedef/value aliases with =
  enum lines
  RealSuperclass by default
```

`::Superclass = ...` 可作为校验或模板折叠兜底，不作为首选数据源。多个 module hierarchy 文件会重复列出依赖类，生成器应按类名去重，并校验去重后的 base/header/module 一致。

模板基类处理规则：

```text
优先使用普通 class line 的 base class；
如果 base class 是模板类型，查找同类的 ::Superclass = X；
如果 X 是非模板 vtk 类型，则使用 X；
如果 X 仍是模板或不存在，则继续跳过模板基类；
找不到可导出的非模板基类时，该类型作为无 C# 基类或报诊断。
```

导出闭包规则：

```text
manifest 中显式导出的类为 explicit export；
为保持继承关系自动补齐的基类为 required export；
导出 A 类时，自动补齐 A 的所有可导出非模板基类。
```

required export 只意味着生成类型骨架和生命周期基础，不意味着自动导出该基类的全部 API。基类方法仍需在该基类 manifest 中显式声明。

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

MVP manifest 采用中等 schema，只覆盖当前阶段必要信息。

必须表达：

```text
class:
  name
  module
  baseType

method:
  name
  nativeName
  visibility
  overloadId
  customShim
  declaredOn
  exposeOn
  reason
  parameters
  returns

parameter:
  name
  type
  direction
  marshal
  lengthFrom / fixedLength / multipleOf

returns:
  type
  ownership
  nullable
  marshal
  managedType
```

暂不表达：

```text
大规模 API 版本范围
自动文档生成
overload 排序策略
平台条件矩阵
泛型或复杂 C# 类型系统映射
```

manifest 条目应明确区分直接绑定 VTK 原方法和 VtkSharp 自定义 shim：

```yaml
- name: GetNumberOfPoints
  returns:
    type: vtkIdType

- name: SetDataFromArray
  customShim: true
  parameters:
    - name: xyz
      type: double[]
      marshal: array
      direction: in
      multipleOf: 3
```

`customShim: true` 表示该 API 是 VtkSharp 为 C# 互操作或工程数据输入提供的补充入口，不应被误认为 VTK C++ 原生方法。

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

MVP 生成器边界：

```text
输入 manifest
输出 C# P/Invoke 声明
输出 C# thin wrapper
输出 ownership / Dispose 基础代码
支持 customShim 方法声明
```

MVP native 边界：

```text
C ABI shim 先手写
custom array copy shim 先手写
native declarations / dispatch 自动生成后置
```

也就是说，第一阶段生成器先覆盖 C# 侧重复度最高的 wrapper 和 P/Invoke 声明。native 侧在规则稳定前保持手写，避免过早引入 C++ 头文件解析、overload 消歧、宏和模板处理复杂度。

### 方法归属

方法默认只在实际声明类上生成，派生类通过 C# 继承获得基类 API。

```text
manifest 当前 class 默认就是 declaredOn；
方法应写在真实声明类下；
MVP 不完整自动推断 declaring class；
轻量 header scanner 只做 best-effort 诊断；
特殊暴露必须显式 declaredOn / exposeOn / reason。
```

如果 manifest 把继承方法写到派生类下，生成器默认报错，而不是自动修正：

```text
Method vtkPolyData.GetMTime is inherited from vtkObject.
Move it to vtkObject, or add explicit exposeOn override if this is intentional.
```

特殊 API 例如派生类重新声明、协变返回值或 VTK 常见 `GetOutput()` 模式，可用显式规则表达：

```yaml
- name: GetOutput
  declaredOn: vtkAlgorithm
  exposeOn: vtkPolyDataAlgorithm
  reason: covariant-return
```

### 函数重载

VTK C++ API 中存在大量函数重载。C# public API 可以保留安全重载，但 manifest 中每个 overload 必须有稳定唯一标识，生成器不能只靠方法名匹配。

推荐 manifest：

```yaml
- name: SetColor
  overloadId: rgb-scalars
  parameters:
    - { name: r, type: double }
    - { name: g, type: double }
    - { name: b, type: double }

- name: SetColor
  overloadId: rgb-array3
  visibility: internal
  parameters:
    - name: rgb
      type: double*
      marshal: fixedArray
      fixedLength: 3
```

C ABI 函数名必须消除重载，例如：

```cpp
VTKSHARP_API void vtkProperty_SetColor_rgb_scalars(vtkProperty* self, double r, double g, double b);
VTKSHARP_API void vtkProperty_SetColor_rgb_array3(vtkProperty* self, const double* rgb);
```

生成器内部方法 identity 至少包含：

```text
class
nativeName
normalized parameter types
const/ref/pointer qualifiers
overloadId
```

primitive pointer、内部数组指针、复杂 C++ 参数等 overload 必须有 marshal rule；否则跳过或报诊断，不生成危险 public API。

### 错误处理与前置条件

public wrapper 做最小前置条件校验，internal P/Invoke 不重复校验，native shim 不做复杂异常转换。

默认异常约定：

```text
null array:
  ArgumentNullException

数组长度不满足 fixedLength / multipleOf:
  ArgumentException

Dispose 后调用 public 方法:
  ObjectDisposedException

New() / factory 返回 IntPtr.Zero:
  InvalidOperationException
```

native shim 可使用 `assert` 表达前置条件。MVP 不做复杂 C++ 异常捕获和跨 ABI 异常转换。

## AI 辅助生成策略

AI 可以参与 manifest 编写，尤其适合分析固定长度数组返回值、常见所有权模式和 VTK 示例转换。

但 AI 只能作为辅助建议来源，不能静默决定内存语义和所有权语义。

推荐半自动流程：

```text
1. 从 official example / engineering scenario 提取 API 需求
2. TypeGraph 自动补齐隐式基类和模块依赖
3. parser / header scanner / hierarchy reader 发现候选 API
4. rule engine 对确定规则的 API 自动生成 generated manifest
5. AI 对不确定 API 给出候选 manifest、证据和风险说明
6. 生成 decisions / diagnostics 文件
7. 人工选择 accepted / rejected / needs-review
8. accepted 决策进入 overrides 或 decisions
9. resolve-manifest 合并生成 resolved manifest
10. generate 根据 resolved manifest 生成代码
```

对于没有 manifest marshal rule 的 `primitive*` 返回值，生成器应报错或跳过，不生成危险的 public pointer API。

建议命令形态：

```bash
vtksharp-gen discover \
  --profile mvp-data \
  --vtk-hierarchy-dir "C:\Program Files\VTK\lib\vtk-9.6\hierarchy\VTK" \
  --out bindings/vtk-9.6/generated

vtksharp-gen resolve-manifest \
  --profile mvp-data \
  --out bindings/vtk-9.6/resolved/mvp-data.resolved.yaml

vtksharp-gen generate \
  --manifest bindings/vtk-9.6/resolved/mvp-data.resolved.yaml
```

自动生成条目可记录轻量元信息：

```yaml
metadata:
  source: generated
  status: needs-review
  confidence: medium
  evidence:
    - header: vtkDataSet.h
    - signature: "void GetBounds(double bounds[6])"
```

人工确认后的覆盖条目：

```yaml
metadata:
  source: override
  status: accepted
  confidence: high
```

MVP 元信息保持克制，优先使用：

```text
source: generated | override | human
status: accepted | needs-review | rejected
confidence: high | medium | low
```

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
run resolve-manifest
run generator
git diff --exit-code
```

确保 generated / overrides / decisions、resolved manifest、生成器和生成代码一致。

## 测试策略

MVP 测试优先验证核心绑定和数据模型，不把窗口渲染或 UI 集成作为硬门槛。

第一类是 managed API 单元测试：

```text
验证托管 wrapper 基础行为
验证 ownership / Dispose 约定
验证数组便利 API 的基本前置条件
```

第二类是 native smoke test，作为 MVP 必须项：

```text
C# 创建 vtkPoints / vtkCellArray / vtkPolyData
写入点、线段、三角面数据
检查 point count / cell count / bounds
验证 owned wrapper 可以按 using / Dispose 释放
```

第三类是 rendering smoke test，作为后续小步：

```text
可考虑 offscreen rendering 生成小图
检查输出非空或基础像素
不作为最早 MVP 的完成条件
```

Windows CI 中 OpenGL、offscreen rendering、GPU/软件渲染环境可能引入额外不确定性。MVP 先用数据构造和 bounds/cell count 检查建立稳定闭环。

MVP 开发和测试阶段的 native runtime 查找策略保持简单：

```text
vtksharp_native.dll 复制到 test output；
VTK runtime DLL 依赖 PATH 或测试启动脚本设置 PATH；
VtkSharp 托管核心只负责 P/Invoke 到 vtksharp_native.dll；
MVP 不实现复杂 NativeLibrary resolver；
MVP 不解决 NuGet runtime assets 分发。
```

本机测试可通过脚本设置 VTK bin 路径：

```powershell
$env:PATH = "C:\Program Files\VTK\bin;$env:PATH"
dotnet test
```

后续 NuGet 分发阶段再讨论 `runtimes/win-x64/native/`、VTK runtime DLL 打包、resolver 和 PATH fallback 的组合策略。

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
    VtkSharp.Wpf/               # future

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
    VtkSharp.Wpf.Tests/         # future

  samples/
    Console/
    Wpf/                        # future

  docs/
    design/
    binding-rules/
    native-build/
    packaging/
```

## 后续待讨论主题

以下内容尚未完整定稿，后续需要继续讨论：

- CMake 与 .NET build 的编排方式；
- native DLL 与 VTK runtime DLL 的 NuGet 分发策略；
- 完整 C++ header parser 或 VTK wrapping metadata 的引入时机；
- WPF render host 的具体实现路线；
- offscreen rendering smoke test 是否进入第二小步；
- VTK 版本升级策略和跨 9.x 兼容策略；
- official examples / engineering scenarios 的优先级清单；
- 示例 API 提取和 C# 转换工具是否作为正式项目组件；
- CI 构建矩阵；
- 文档站点和示例组织方式。
