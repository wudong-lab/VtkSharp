# VtkSharp 公开绑定与 BRDI 私有扩展架构设计

## 1. 状态

- 状态：已确认
- 日期：2026-08-10
- 范围：仓库边界、native 聚合方式、程序集与命名空间约定、后续构建扩展点
- 本文只记录设计决策，不要求立即修改源码、项目文件或目录结构。

## 2. 背景

`VtkSharp` 当前采用 C ABI shim + C# P/Invoke 封装 VTK，并将静态 VTK 链接进 `VtkSharp.Native.dll`。项目中还存在 WPF 渲染互操作代码，后续也会增加 BRDI 自行实现的 VTK 派生类型、业务可视化能力和其他私有扩展。

长期目标是同时满足：

- `VtkSharp` 保持纯粹、通用和开源，只封装 VTK 官方 API。
- BRDI 私有代码不进入公开仓库。
- 私有项目能够增加自定义 VTK 类型、WPF 集成和业务功能。
- 继续使用静态 VTK，保持 Windows 部署简单，避免发布大量 VTK 动态库。
- 官方 wrapper 与自定义类型在命名空间上可以显式区分。

## 3. 核心决策

建立两个独立 GitHub 仓库：

1. 公开仓库 `VtkSharp`。
2. 私有仓库 `BRDI.VtkSharp`。

`BRDI.VtkSharp` 通过 Git submodule 引用公开 `VtkSharp` 的源码。该依赖是源码级依赖，不是对公开构建产物的二进制依赖。

两个仓库均可独立配置和编译，并分别形成独立发行物：

```text
公开发行物：
VtkSharp.dll
VtkSharp.Native.dll       # 只包含官方 VTK API shim 和一份静态 VTK

BRDI 私有发行物：
VtkSharp.dll              # 从固定版本的公开源码构建，只包含官方 wrapper
VtkSharp.Native.dll       # 官方 shim + BRDI native 扩展 + WPF native + 一份静态 VTK
BRDI.VtkSharp.Extensions.dll
BRDI.VtkSharp.Wpf.dll
BRDI.<Product>.Visualization.dll
```

公开版和私有版的 `VtkSharp.Native.dll` 是两种并列的产品变体。BRDI 业务程序只使用私有仓库构建的整套产物，不引用或部署公开仓库生成的 DLL。

## 4. 为什么私有聚合 DLL 保留原名

私有聚合 native DLL 仍命名为：

```text
VtkSharp.Native.dll
```

官方接口的 C# 封装仍命名为：

```text
VtkSharp.dll
```

这样可以直接复用现有官方 wrapper 中的 P/Invoke 约定：

```csharp
[DllImport("VtkSharp.Native")]
```

无需参数化或批量修改：

- 官方 managed assembly 名称。
- 官方 native library 名称。
- 官方 wrapper 的 `DllImport`。
- `InteropInfo.NativeLibraryName`。
- 官方 wrapper 的命名空间。
- native target 名称。

虽然公开版和私有版 DLL 文件名相同，但它们不会在同一个 BRDI 业务程序中并列部署。私有版是公开标准 runtime 的源码派生聚合版本，而不是与公开 runtime 同时加载的插件。

## 5. 静态 VTK 与 native 聚合边界

BRDI 私有发行物中只允许存在一个包含 VTK 的 native DLL：

```text
VtkSharp.Native.dll
├─ 官方 VTK API 的 C ABI shim
├─ BRDI 自定义 VTK 类型
├─ BRDI 自定义 native 导出
├─ WPF native 渲染互操作
└─ 一份静态 VTK
```

不采用以下结构：

```text
VtkSharp.Native.dll             # 静态链接一份 VTK
BRDI.VtkSharp.Native.dll        # 再静态链接一份 VTK
```

如果两个 DLL 分别静态链接 VTK，再在它们之间传递 `vtkRenderer*`、`vtkActor*`、`vtkRenderWindow*` 或自定义派生对象，会形成两套 VTK runtime 之间的对象交互。其风险包括对象工厂、模块自动初始化、全局状态、RTTI、引用计数和生命周期不一致。因此，所有需要与 VTK 对象交互的 BRDI native 代码都编译进私有聚合版 `VtkSharp.Native.dll`。

纯粹不接触 VTK 对象的 native 辅助代码理论上可以拆成其他 DLL，但初始方案不主动增加这一二进制边界。

## 6. 仓库职责

### 6.1 公开仓库 `VtkSharp`

公开仓库负责：

- VTK 官方 public API 的 C ABI 导出。
- VTK 官方 API 的 C# wrapper。
- wrapper generator、白名单、schema 和生成器测试。
- 通用生命周期、事件和互操作基础设施。
- 与官方绑定相关的测试和最小示例。
- 允许外部源码聚合构建的通用 CMake 扩展点。

公开仓库不包含：

- BRDI 自定义 VTK 类型。
- BRDI 业务算法和业务数据结构。
- BRDI 产品功能。
- 仅服务于 BRDI 产品的 WPF 控件和示例。
- 对私有仓库路径、类型名或产品名的硬编码。

### 6.2 私有仓库 `BRDI.VtkSharp`

私有仓库负责：

- 固定公开 `VtkSharp` submodule commit。
- 驱动私有聚合 native 构建。
- BRDI 自定义 VTK 派生类型及其 C ABI 导出。
- 自定义类型的 C# wrapper。
- WPF 控件和 WPF native 渲染互操作。
- BRDI 业务可视化扩展。
- 私有测试、示例、打包和发布流程。

私有仓库不得修改 submodule 工作树来保存长期补丁。公开侧需要的通用构建能力应提交到公开仓库；BRDI 具体实现保留在私有仓库。

## 7. 程序集与命名空间

官方 wrapper 即使由私有仓库构建，仍保持：

```text
程序集：VtkSharp.dll
命名空间：VtkSharp
```

BRDI 自定义类型使用组织命名空间：

```text
程序集：BRDI.VtkSharp.Extensions.dll
命名空间：BRDI.VtkSharp
```

WPF 扩展使用：

```text
程序集：BRDI.VtkSharp.Wpf.dll
命名空间：BRDI.VtkSharp.Wpf
```

具体业务功能可继续按产品拆分：

```text
程序集：BRDI.<Product>.Visualization.dll
命名空间：BRDI.<Product>.Visualization
```

示例调用关系：

```csharp
using VtkSharp;
using BRDI.VtkSharp;

var renderer = vtkRenderer.New();
var actor = vtkCustomScalarBarActor.New();
renderer.AddActor(actor);
```

该命名方式可以直接区分：

- `VtkSharp.*`：VTK 官方 API 的 .NET 封装。
- `BRDI.VtkSharp.*`：BRDI 自定义 VTK 类型和扩展能力。

BRDI 新增 C ABI 导出建议使用 `BRDI_` 前缀，避免与当前或未来的官方生成导出重名，例如：

```cpp
VTKSHARP_API vtkCustomScalarBarActor*
BRDI_vtkCustomScalarBarActor_New();
```

## 8. 自定义 VTK 类型

BRDI 新增的是独立派生类型，不修改或重复定义 VTK 官方类型。示例：

```cpp
class vtkCustomScalarBarActor : public vtkScalarBarActor
{
public:
    static vtkCustomScalarBarActor* New();
    vtkTypeMacro(vtkCustomScalarBarActor, vtkScalarBarActor);

protected:
    vtkCustomScalarBarActor() = default;
    ~vtkCustomScalarBarActor() override = default;
};
```

不得在同一 VTK runtime 中重新定义官方已存在的同名类型，例如再次定义 `vtkScalarBarActor`。

私有 managed wrapper 可以继承官方 wrapper。由于两者最终调用同一个私有聚合 `VtkSharp.Native.dll`，自定义对象可以直接传递给官方 renderer、mapper 或其他 VTK 对象。

## 9. 后续需要提供的构建扩展点

在不改变公开默认构建结果的前提下，公开 native 构建后续需要提供通用扩展能力。概念上至少包括：

```text
VTKSHARP_EXTRA_NATIVE_SOURCES
VTKSHARP_EXTRA_NATIVE_HEADERS
VTKSHARP_EXTRA_VTK_COMPONENTS
VTKSHARP_EXTRA_NATIVE_LIBRARIES
```

私有顶层 CMake 在加入公开 submodule 前设置这些变量，再由公开 native target 统一消费。

额外 VTK modules 必须使用同一份最终集合驱动：

1. `find_package(VTK COMPONENTS ...)`
2. `target_link_libraries(...)`
3. `vtk_module_autoinit(...)`

不能只在私有 target 上补链接而遗漏模块发现或自动初始化。

当前 `VtkSharp.Native/CMakeLists.txt` 受 generator 输出约束。正式实施扩展点时，需要同步修改 generator 的 CMake emitter/template 和测试，避免重新生成绑定时覆盖扩展能力。

## 10. WPF 扩展边界

WPF managed 控件和 WPF native 互操作迁移到 `BRDI.VtkSharp` 私有仓库。凡是直接使用 VTK C++ API或持有 VTK native 对象的 WPF native 代码，都编译进私有聚合 `VtkSharp.Native.dll`。

`vtkWin32OpenGLDXRenderWindow` 属于 VTK 官方 API，因此其基础 wrapper 可以继续属于公开 `VtkSharp`。是否使用该类替换当前自研 WGL/OpenGL 互操作，是后续独立实现决策，不在本文中直接确定。

如果后续采用该类，仍需注意 WPF `D3DImage` 接受 `IDirect3DSurface9`，而 `vtkWin32OpenGLDXRenderWindow` 使用 D3D11 texture；BRDI WPF native 层仍需承担 D3D11 与 D3D9Ex 的共享资源适配。

## 11. 打包与版本识别

私有项目不引用公开 NuGet runtime 包。私有发行流程自行打包：

```text
VtkSharp.dll
VtkSharp.Native.dll       # BRDI 聚合版本
BRDI.*.dll
```

由于标准版和私有聚合版 native DLL 文件名相同，私有版应提供可识别的版本信息，例如：

```text
ProductName: BRDI VtkSharp Runtime
FileDescription: VtkSharp Native Runtime with BRDI Extensions
InformationalVersion: <VtkSharp commit>+brdi.<extension version>
```

私有发布清单至少记录：

- `VtkSharp` submodule commit。
- VTK 版本和构建标识。
- BRDI 扩展版本。
- MSVC toolset、平台和配置。
- native DLL hash。

## 12. 验证要求

### 12.1 公开仓库

- 不提供任何额外参数时，构建结果仍为标准 `VtkSharp.dll` 和 `VtkSharp.Native.dll`。
- 公开测试、生成器检查和最小示例继续通过。
- 公开构建不包含任何 BRDI 类型、导出或路径。

### 12.2 私有仓库

- 输出目录中只有一份 `VtkSharp.Native.dll`。
- 该 DLL 同时包含官方导出和 `BRDI_` 私有导出。
- 该 DLL 不依赖另一个 `VtkSharp.Native.dll`。
- 私有项目未通过 NuGet 或其他传递依赖引入公开 runtime 包。
- `VtkSharp.dll` 与记录的 submodule commit 一致。
- 自定义 VTK 对象可以加入官方 renderer、参与渲染并正确释放。
- WPF 控件应验证初始化、resize、重复加载/卸载、关闭重开、设备或前缓冲区失效恢复。
- Debug/Release、VTK、CRT、MSVC toolset 和 x64 平台必须匹配。

## 13. 不采用的方案

### 13.1 两个独立 native DLL 分别静态链接 VTK

不采用。两个 runtime 之间需要传递 VTK 对象，生命周期和全局状态风险不可接受。

### 13.2 立即切换到动态 VTK

暂不采用。动态 VTK 能支持真正独立的 native 插件 DLL，但会显著增加 DLL 数量、部署体积、版本协调和打包复杂度。当前需求可以通过私有聚合 DLL 在静态 VTK 下满足。

### 13.3 将私有聚合 DLL 改名为 `BRDI.VtkSharp.Native.dll`

暂不采用。BRDI 业务项目不会部署公开 runtime，因此保留 `VtkSharp.Native.dll` 不会形成运行时歧义，同时可以避免参数化所有官方 P/Invoke 库名。

### 13.4 将全部官方 wrapper 命名空间改为 `BRDI.VtkSharp`

不采用。当前源码包含显式 `namespace VtkSharp`，编译时不能仅通过 `RootNamespace` 自动替换。保留官方 namespace 可以直接复用源码和示例；BRDI 新增类型使用 `BRDI.VtkSharp` 即可形成清晰边界。

## 14. 推荐实施顺序

本文确认设计，不立即实施。后续如开始改造，建议按以下顺序执行：

1. 在公开 native 构建中加入通用、默认无行为变化的额外源码与 VTK module 扩展点。
2. 同步更新 generator 的 CMake emitter 和测试。
3. 建立私有 `BRDI.VtkSharp` 仓库，并以 submodule 固定公开源码版本。
4. 先构建不含业务扩展的私有聚合 `VtkSharp.Native.dll`，验证与标准构建等价。
5. 加入一个最小自定义 VTK 派生类型，验证创建、官方 API 调用、渲染和释放。
6. 迁移 WPF managed/native 代码及其示例和测试。
7. 建立私有打包、版本清单和防混用检查。

每个阶段分别验证，避免把仓库迁移、生成器调整、native 聚合、WPF 渲染和业务类型扩展放在一次变更中完成。
