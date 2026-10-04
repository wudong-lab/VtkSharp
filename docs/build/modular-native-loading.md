# 模块化 native 导出层与按需加载设计

记录日期：2026-10-04。状态：设计决策及阶段一至六当前范围的实施已完成；私有独立 NuGet 包、CAD 宿主同名 DLL 冲突和真实 GPU 设备丢失恢复属于后续验收项。本文同时保留未覆盖的环境边界和验证限制。

当前架构见 [项目架构](../architecture.md)，构建与部署入口见 [VtkSharp 构建](vtksharp.md)。

## 1. 目标与范围

将公开 C ABI 导出层组织为多个 native DLL，在第一次使用对应功能时加载入口及其必要依赖。私有扩展暂不分组，保持输出单一 `BRDI.VtkSharp.Native.dll`，待公开封装稳定后另行讨论模块化。上层业务继续使用 C# wrapper，无须逐个手动加载 VTK 模块。

公开仓库为 `D:\Code\wudong-lab\VtkSharp`；私有扩展仓库为 `D:\Code\wudong-lab\VtkSharpInternal`。两侧共同参与改造，共享同一套动态 VTK runtime。

本次目标是减少未使用功能的运行时加载，并统一 native 部署目录。按需加载不等于按需部署，也不保证只加载业务直接调用的 DLL：VTK 自身的普通导入依赖仍会随模块加载。DLL 已加载也不等于全部文件内容立即进入物理内存，收益需要测量。

## 2. 已确认的设计决策

| 决策 | 约定 |
| --- | --- |
| native 架构 | 采用模块化 native 导出层；不以单入口 `/DELAYLOAD` 作为主方案 |
| 分组关系 | VtkSharp native DLL 与 VTK DLL 不要求一一对应；允许显式合并多个模块 |
| 分组依据 | 官方元数据、版本化分组策略和实际 DLL 依赖分析共同决定结果 |
| 生成规则 | 默认模块独立；合并必须显式配置，不能因共享依赖自动合并 |
| 旧 C ABI | 不需要兼容旧 C ABI，不增加旧入口的完整导出转发层 |
| 私有扩展 | 纳入加载、部署和兼容性改造；暂不分组，保持单一 `BRDI.VtkSharp.Native.dll`；公开与私有入口的加载顺序均需验证 |
| 私有 NuGet | 后续生成独立私有 NuGet 包，显式依赖兼容的公开 VtkSharp 包版本 |
| 部署目录 | 默认 `native/VtkSharp/win-x64/`；导出入口、VTK DLL 和第三方依赖在该目录内平铺 |
| 额外 VTK 依赖 | 私有包补充公开包未交付的必要 VTK DLL 及第三方依赖，合并为同一份 runtime |
| native 依赖边界 | 私有 native 直接链接 VTK，不直接链接公开 native 导出 DLL；私有 managed 仍依赖公开 wrapper |
| 确定性 | 相同规范化输入和策略版本生成相同结果；依赖变化必须可审查 |

`VtkSharp` 子目录名称用于避免与第三方 NuGet 包的输出目录重名。它不能隔离同一进程中已经加载的不兼容同名 VTK DLL。

## 3. 已确认的首阶段实施范围

首阶段实施范围如下；最终成员、配置 API 和加载实现见第 8 节实施记录。

| 事项 | 已确认方案 | 实施边界与后续工作 |
| --- | --- | --- |
| 首版合并集合 | 普通模块独立，仅合并经分析确定的渲染初始化组，暂不合并整个 Common Kit | 沿用当前 OpenGL2 后端；准确成员由元数据、私有 WPF 后端和原型验证确定 |
| 消费支持范围 | 首阶段验证 Windows x64、.NET 8 和 .NET Framework 4.8 的 SDK 风格 `PackageReference` 项目 | 非 SDK 项目、`packages.config` 和其他架构暂不纳入首阶段 |
| runtime 定位 | 普通应用以 `AppContext.BaseDirectory` 为基准，允许首次加载前指定 runtime 根目录 | 配置 API 在实施时确定；CAD 宿主通过显式配置定位，加载冲突另行验证 |
| 交付范围 | 首阶段交付完整 runtime，由加载器按需加载 | 暂不按 VTK 功能模块拆分 NuGet 包或交付裁剪工具；后续独立私有包遵循第 6 节约定 |
| 失败与卸载 | 打包验证完整闭包；运行时检查当前请求组，失败报告模块、路径和原因；已加载模块保留到进程退出 | 暂不支持动态卸载和加载后切换 runtime |

加载路径、P/Invoke 绑定方式和工厂初始化入口已通过原型及集成验证，环境边界见第 7 节。

## 4. 官方依据与当前依赖分析

### 4.1 VTK 模块元数据

VTK 的 `vtk.module` 提供公开依赖 `DEPENDS`、内部依赖 `PRIVATE_DEPENDS`、可选依赖 `OPTIONAL_DEPENDS`、Kit 归属 `KIT` 和工厂关系 `IMPLEMENTS`、`IMPLEMENTABLE`。Kit 用于聚合相关模块形成库；`GROUPS` 用于批量启用构建模块，不是运行时 DLL 分组规则。依据见 [VTK 模块文件与 Kit 文档](https://docs.vtk.org/en/v9.7.0/api/cmake/vtkModule.html#vtk-module-file-contents)。

对象工厂通过实现模块注册具体实现，使用方需要正确配置 autoinit。依据见 [VTK autoinit 文档](https://docs.vtk.org/en/v9.7.0/api/cmake/vtkModule.html#leveraging-the-autoinit-subsystem) 和 [对象工厂说明](https://docs.vtk.org/en/latest/advanced/object_factory.html)。这些关系约束加载及注册时序，不强制 C ABI 导出实现必须合并为一个 DLL。

当前安装的 `VTK-vtk-module-properties.cmake` 保留依赖和工厂关系。建议通过已配置的 CMake target 属性提取规范化快照，源码中的 `vtk.module` 和 `vtk.kit` 作为归属及解释依据。不要将简单文本扫描当作平台条件和实际构建配置的替代。

### 4.2 当前构建的具体证据

分析基线为 VTK 9.7.0、Windows x64、Shared Release。当前安装目录是 `D:\Code\VTK\VtkGitBuild-shared\install\Release\lib\cmake\vtk-9.7`；这只是本次分析位置，不作为生成结果中的固定路径。

| 模块 | 已核对的关系 |
| --- | --- |
| CommonCore | 源码声明属于 `VTK::Common` Kit |
| FiltersSources | 源码声明属于 `VTK::Filters` Kit，依赖数据模型和执行管线等模块 |
| RenderingCore | 源码声明属于 `VTK::Rendering` Kit，支持实现模块注册 |
| RenderingOpenGL2 | 源码声明属于 `VTK::OpenGL` Kit，注册到 RenderingCore 和 RenderingLabel |
| InteractionStyle、RenderingUI | 安装元数据声明注册到 RenderingCore |

当前公开 native 构建生成的 autoinit 头文件包含：

```cpp
#define vtkRenderingCore_AUTOINIT 3(vtkInteractionStyle,vtkRenderingOpenGL2,vtkRenderingUI)
#define vtkRenderingLabel_AUTOINIT 1(vtkRenderingOpenGL2)
```

这支持将选定的渲染实现统一管理初始化，但最终组成员还应检查其他提供者、私有 WPF 后端及实际源码需求。不能照搬当前全入口 autoinit 到所有新入口。

按 Release 模块运行时依赖清单中的 `ordinary-import` 和 `application-file` 边计算闭包，得到：

| 底层入口 | 闭包文件数，包含入口 | 闭包中的 IO 模块 |
| --- | ---: | --- |
| vtkFiltersSources-9.7.dll | 23 | 无 |
| vtkIOXML-9.7.dll | 21 | IOCore、IOXML、IOXMLParser |
| vtkRenderingOpenGL2-9.7.dll | 47 | IOCore、IOImage |

计数包含第三方运行 DLL，不包含系统 DLL，只代表该构建产物的普通导入闭包。OpenGL2 使用时会加载 IOImage，即使其 C ABI 导出 DLL 保持独立。这些闭包不等于建议的导出层合并集合，也不覆盖全部显式动态加载行为。

## 5. 确定性分组与生成规则

1. 每个已封装 VTK 模块默认拥有一个独立导出目标；新增模块不自动改变已有归属。
2. 分组策略显式列出组名、成员、选定实现模块和规则依据。Kit 是合并候选，不自动整体合并；模块名称前缀、目录和 `GROUPS` 不作为隐式合并规则。
3. 普通依赖形成有向边，不因共同依赖或依赖传递合并导出目标。依赖环需要报告和分析，不直接当作导出层必须合并的证明。
4. 工厂实现由策略显式选择；依据元数据检查提供者及注册关系，不能加载安装目录中所有实现后端。
5. 底层依赖没有封装导出时只作为 runtime 依赖，不为其生成空导出 DLL。需要额外初始化代码时，必须记录用途及所属目标。
6. 每个生成导出、手写辅助源码和通用互操作实现必须有唯一归属。跨模块继承、对象指针传递本身不触发合并。
7. 原有全局 `RuntimeModules` 需要迁移为组级链接或初始化需求，避免每个入口继续链接全部 runtime 模块。
8. 每组独立生成链接目标和 autoinit 配置；额外手写导出所需模块必须参与查找、链接和初始化需求分析。

生成输入包括 VTK 版本及元数据快照、相关构建选项、平台、白名单、手写源码归属、分组和后端策略版本、生成器版本。Debug/Release 的 DLL 文件名与 runtime 清单按各自配置解析，逻辑分组仅在相关输入相同的前提下保持一致。

建议人工维护策略文件，生成模块归属与初始化清单。文件名和 schema 在实施时确定；清单应记录模块、组、导出目标、直接链接需求、初始化提供者及规则来源。所有集合固定去重、排序，文本使用统一编码和换行；不写生成时间、机器绝对路径或文件枚举顺序。带时间和来源路径的部署诊断清单与确定性生成文件分别管理。

未知模块、重复归属、组名冲突、未满足的实现需求和元数据不匹配应在生成或配置阶段明确失败。VTK 升级需要审查快照与策略差异，不能静默重新分组。

## 6. 子目录部署与加载机制

目标输出布局如下，模块名称仅作示意：

```text
MyApp/
  MyApp.exe
  VtkSharp.dll
  BRDI.VtkSharp.dll
  BRDI.VtkSharp.Wpf.dll
  native/VtkSharp/win-x64/
    VtkSharp.Native.CommonCore.dll
    VtkSharp.Native.FiltersSources.dll
    VtkSharp.Native.Rendering.dll
    BRDI.VtkSharp.Native.dll
    vtkCommonCore-9.7.dll
    ...
    VtkSharp.native-dependencies.json
    BRDIVtkSharp.native-dependencies.json
    native-dependencies.json
```

### 6.1 包归属与共享部署目录

公开包负责公开 managed/native 封装及基础 VTK runtime。后续独立私有包负责私有 managed/native 封装、私有专用依赖和公开包缺少的 VTK 依赖，并显式依赖兼容的公开包版本。私有包名在实施时确定。

两包的全部 native 资产统一部署到 `native/VtkSharp/win-x64/`，不再使用独立的 `native/BRDIVtkSharp/win-x64/`。该目录属于应用的共享部署布局，不由公开包独占。私有包携带的文件可以部署到这个目录，包归属不要求与输出目录名称一致。

显式包依赖保证公开包参与还原，不保证部署目录已经创建。两包的部署规则均需能够创建目标目录，不依赖复制顺序。包只读取自己的 NuGet 资产，不向另一个包的缓存目录写入文件。各包只声明和清理自身文件，禁止递归删除整个共享目录。

私有 native 入口直接调用 VTK C++ API，不通过公开 C ABI 作为中间层。私有 C# wrapper 仍可能通过继承的公开方法、对象释放和通用互操作调用公开 native 模块，因此应用仍需部署对应公开模块。共享 runtime 不改变这两个层次的依赖边界。

私有扩展保留单一 native target 和入口，不增加私有分组策略或模块级 P/Invoke 名称。首次使用该入口时，其全部普通导入依赖仍会一起加载；首阶段不要求私有内部功能达到公开模块的加载粒度。独立私有 NuGet 包和统一目录部署不以私有模块化为前提。

### 6.2 额外依赖与部署清单

私有扩展可能依赖公开封装白名单之外的 VTK 模块。收集器需从私有入口独立计算完整需求闭包，包含普通导入、延迟导入和显式初始化或动态加载需求，不能仅复用公开包的文件列表。

私有包补充集合为私有需求闭包中未由指定公开包提供的运行文件。按文件名、哈希及构建身份核对：同名同内容可去重，同名不同内容必须失败，不能视为可覆盖的补充文件。补充集合绑定公开包版本和 runtime 清单，公开包升级后需重新校验。

补充的 VTK DLL 必须来自与公开 runtime 匹配的构建。新增模块若要求改变 VTK 构建配置，需要重新验证共享依赖和 ABI，必要时同步更新两侧基线，不能只从另一份安装目录拷贝缺少的 DLL。

包级清单分别命名为 `VtkSharp.native-dependencies.json` 和 `BRDIVtkSharp.native-dependencies.json`。消费应用的统一部署步骤合并两包声明，验证版本、build id、配置、文件哈希、依赖闭包和目标路径冲突，形成最终 `native-dependencies.json`，记录每个文件的提供包与依赖依据。

部署校验应在复制前汇总全部目标文件，以 Windows 文件名比较规则检查重名。相同文件去重为一个复制项，不同文件报错；最终清单由统一步骤生成，不由两包分别写入同名文件。构建、发布和运行时加载使用同一部署目录配置。

### 6.3 NuGet 部署与按需加载

NuGet 包内路径和最终安装路径分别设计。SDK 会扁平化 `runtimes/{rid}/native/` 内的目录结构，不能仅添加包内子目录就保证安装布局。建议把自管 runtime 资产放在独立包目录，通过 `buildTransitive/VtkSharp.targets` 加入构建和发布的复制项，明确目标路径和架构选择，避免同时作为标准 native 资产重复复制或被 `.deps.json` 解析到另一份 runtime。依据见 [NuGet native 文件规则](https://learn.microsoft.com/en-us/nuget/create-packages/native-files-in-net-packages) 和 [包内 MSBuild 规则](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets)。

私有包提供自己的 `buildTransitive/<PackageId>.targets`，遵循公开包定义的共享部署契约。多个包向同一应用输出目录部署文件属于支持的自定义构建行为；NuGet 没有要求输出目录与包名对应。应用仅引用私有包时，也需验证公开依赖包的传递构建规则参与部署。统一目录无需为公开、私有入口增加跨目录依赖搜索。

按绝对路径加载导出入口，并正确配置依赖搜索目录。Windows 原型可使用 `LoadLibraryExW` 配合 `LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32`，使入口目录中的依赖参与本次解析；后续独立的动态加载也要遵循同一规则。依据见 [LoadLibraryExW 文档](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw)。不通过修改全局 `PATH` 或进程当前目录定位 runtime。

.NET 8 在首次 P/Invoke 前注册 assembly 级 resolver，返回按绝对路径加载并缓存的句柄。.NET Framework 4.8 在调用前预加载对应入口；两种绑定路径均已通过隔离消费验证。依据见 [.NET native 加载说明](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading)。

首次加载和注册需要同步，避免公开、私有入口并发初始化时重复或遗漏注册。不能把新增加载逻辑放入 `DllMain`。需检查并约束现有 autoinit 的执行时机，不能将“文件已加载”直接视为“所有工厂已按预期注册”。正常调用复用模块句柄和已解析函数地址。

公开和私有入口必须共享 VTK 版本、build id、架构、配置和兼容 CRT；继续保留对象引用计数、指针归属和回调保活约定。子目录不能解决 CAD 宿主中已加载同名异内容 DLL 的冲突，需要单独验证。

## 7. 当前验证状态

已完成：公开生成器模块化输出、VTK 元数据校验、Release native target 与 NuGet 包构建；.NET 8 和 .NET Framework 4.8 的隔离消费、publish 与目录搬迁验证；Cone、XML IO、公开/私有入口对象交互；私有 Debug/Release 构建、WPF smoke 与两配置测试；缺失依赖/同名异内容诊断及单入口与模块化入口的本地性能对比。细节和边界见第 8 节。

当前范围内尚未验证 CAD 宿主启动时已加载不兼容同名 VTK DLL 的冲突，也未在真实 GPU 设备丢失后恢复 WPF D3DImage；这需要目标宿主和设备环境。独立私有 NuGet 包属于后续交付，不是本轮私有工程集成的前置条件。

## 8. 实施记录

### 阶段一：基线核对与规则冻结（2026-10-04）

- 两个仓库在本轮开始时均无未提交工作区变更。
- 公开当前仍由 `VtkSharp.Native.dll` 单入口承载全部 29 个白名单模块；根配置额外全局链接 CommonDataModel、RenderingOpenGL2、InteractionStyle 和 RenderingUI。
- 私有当前由单一 `BRDI.VtkSharp.Native.dll` 直接链接 VTK，CMake 查找/链接/autoinit 列表包含 CommonCore、CommonDataModel、CommonExecutionModel、RenderingAnnotation、RenderingCore、RenderingOpenGL2。
- 首版策略已落在 `src/generator/config/vtksharp.native-modules.yml`：普通模块默认独立；显式渲染组包含 InteractionStyle、RenderingCore、RenderingLabel、RenderingOpenGL2、RenderingUI；工厂提供者显式限定为 InteractionStyle、RenderingOpenGL2、RenderingUI；手写 UTF-8/string 辅助导出归属 CommonCore。
- 该渲染集合沿用 OpenGL2 后端，并对齐当前 autoinit 关系。RenderingAnnotation 和 InteractionWidgets 保持独立，VTK 普通依赖不会自动合并 C ABI target。私有 WPF 后端仍是独立入口，直接链接 VTK。
- VTK Release 安装与当前公开 runtime 清单均为 9.7.0、Windows x64、Release、build id `54f2e9a1722a89e1a792448fb8e8585ded55a2c5c8137ec48f784e3b1a9d98f2`；公开基线清单有一个入口和 73 个运行文件。阶段一沿用第 4 节已有的模块关系、autoinit 和 PE 闭包分析证据。
- 私有 `AGENTS.md` 已按当前实际入口名及共享目录、依赖边界约定修订；加入加载规则及同步初始化验证要求。

阶段一冻结的是可审查的首版策略，不代表渲染组实现与 .NET Framework 4.8 加载路径已经通过运行原型。阶段二须先验证 CommonCore、FiltersSources 和 Rendering 组的真实构建及两个托管运行时加载，再扩大生成器迁移范围。

### 阶段二：最小加载原型（2026-10-04）

- 原型源码和运行脚本位于 `tools/module-loading-prototype/`。
- 原型使用匹配的 VTK 9.7.0 Shared Release x64 build id `54f2e9a1722a89e1a792448fb8e8585ded55a2c5c8137ec48f784e3b1a9d98f2`，包含 CommonCore、FiltersSources、Rendering 和现有私有 `BRDI.VtkSharp.Native.dll` 四个入口，合并依赖清单共 54 个文件。
- .NET 8 assembly resolver 在首次 P/Invoke 时使用绝对路径调用 `LoadLibraryExW`，设置 `LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32` 并缓存句柄；.NET Framework 4.8 在 P/Invoke 前按绝对路径预加载对应 DLL，CLR 随后绑定已加载模块。两者均不修改 PATH，也不卸载模块。
- 每个场景独立启动进程，且运行期间清空 VTK 环境变量并将 PATH 限定为 Windows 系统目录。所有检查到的 native 模块均从原型 runtime 目录加载。
- .NET 8 和 .NET Framework 4.8 均通过 CommonCore 数据对象、FiltersSources 球体半径读写、Rendering 工厂、私有先/公开先调用以及并发首次加载六个场景。Rendering 工厂返回 `vtkWin32OpenGLRenderWindow`。私有创建的 `vtkBrdiContourLegendActor*` 被公开 CommonCore 入口接收并查询类名，两个顺序都成功。
- 实际进程加载结果：CommonCore 场景载入 6 个 VTK DLL；FiltersSources 场景载入 23 个 VTK DLL 和两个 C ABI 入口（FiltersSources、CommonCore，后者承担通用对象释放）；Rendering 场景载入 48 个 VTK DLL 和两个 C ABI 入口（Rendering、CommonCore）；私有场景载入 49 个 VTK DLL和两个 C ABI 入口（私有入口、CommonCore）。渲染闭包实际包含 IOCore、IOImage 等普通导入依赖，证明 C ABI 分组不会裁剪 VTK 自身依赖。
- 日志记录了每个场景的首次调用耗时、工作集和完整加载路径。原型计时包括托管启动及 JIT，样本数也不足以推断启动收益；不作为性能结论，阶段六再做重复对比。

阶段二已确认两种托管加载路径在当前 Windows x64 环境可行，以及私有/公开入口可以共享同一 VTK runtime 并发初始化。原型只创建 RenderWindow 并验证工厂结果，没有执行实际窗口渲染、NuGet 子目录部署或 CAD 宿主冲突验证；这些仍属于后续阶段。

### 阶段三：公开生成器与导出层迁移（2026-10-04）

- 新增 `src/generator/config/vtksharp.native-modules.yml` 和对应 schema，普通模块默认独立，Rendering 显式合并五个模块，并将三个工厂提供者单独列为组级 autoinit；手写 `vtksharp_string.cpp` 明确归属 CommonCore。
- 新增 CMake target 属性快照导出工具 `tools/vtk-module-metadata/`、schema 和当前 VTK 9.7.0 Windows x64 Release 快照。生成器校验快照版本、平台、模块集合、工厂提供者与可实现 target 的覆盖关系，并将规范化元数据纳入增量指纹。
- C# 生成器现按模块写入 P/Invoke 库名，并确保首次调用前对应入口已准备加载；.NET 8 使用 assembly resolver，.NET Framework 4.8 通过静态构造器预加载。新增 `VtkSharpRuntime.ConfigureNativeRuntimeDirectory` 用于首次调用前配置 runtime 目录。
- 生成结果为 26 个独立/分组 C ABI DLL；CMake 每个 target 分别链接自身 VTK 模块集合及对应 autoinit。手写 wrapper 类不会被错误加入生成源码，UTF-8 字符串复制辅助实现由公共头文件内联提供，释放导出仍归 CommonCore。
- 当前 VTK Shared Release x64 全部 native targets 构建成功，managed `netstandard2.0` 与 `net8.0` 均构建成功。多入口依赖收集器生成含 98 个文件的闭包清单；生成器测试 246 项通过。

阶段三的 native 构建和清单生成已验证。新生成 wrapper 的隔离 NuGet 消费应用验证结果记录在阶段四。

### 阶段四：NuGet 与部署迁移（2026-10-04）

- 公开 NuGet 包将 native 资产放入包内 `native/VtkSharp/win-x64/`，通过 `buildTransitive/VtkSharp.targets` 复制到消费应用的同一相对目录；build 与 publish 都保留该路径，包级清单命名为 `VtkSharp.native-dependencies.json`。
- `build-native.ps1` 从全部模块入口收集依赖闭包；`package-nuget.ps1` 按 manifest `inputSources` 映射多入口源目录，运行文件名、hash、VTK build id 和许可复制校验。当前 runtime 共 98 个文件（约 86.3 MB），包括 26 个 C ABI 入口。
- 生成本地验证包 `VtkSharp.26.1004.905`，使用独立 `PackageReference` 消费项目分别验证 .NET 8 和 .NET Framework 4.8。两个项目 build 与 publish 输出根目录均无 VtkSharp native DLL，子目录含 26 个入口 DLL 和同一份 runtime；算法运行及释放均成功，Cone 输出 7 个点。
- 两个 publish 输出的运行测试清除了 `VTK*` 环境变量，并将 PATH 限定到 Windows 系统目录；两种框架均从应用 `native/VtkSharp/win-x64/` 目录加载，没有依赖开发安装或 PATH。

阶段四完成时，公开包消费路径已验证；私有 runtime 合并与冲突诊断在阶段五、六补充验证。验证时使用的包曾保存在本地 ignored `artifacts/` 中，尚未发布。

### 阶段五：私有扩展与 WPF 迁移（2026-10-04）

- 私有 CMake 仍只有 `BRDI.VtkSharp.Native.dll` 一个 target，直接链接共享 VTK；依赖收集器读取公开包的 26 个入口，与私有入口合并为 27 个入口、99 个文件。私有需求未引入公开 26 个模块包之外的额外 VTK/第三方 DLL；同名文件哈希一致。
- 私有 Core 和 WPF managed assembly 各自在 .NET 8 注册 assembly resolver、在 .NET Framework 4.8 P/Invoke 前用 `LoadLibraryExW` 绝对路径预加载。两个配置入口默认指向 `AppContext.BaseDirectory/native/VtkSharp/win-x64`；加载后句柄保留到进程退出。
- 两个私有项目锁定公开 `VtkSharp.26.1004.905`。调试构建验证发现包的 `buildTransitive` 规则会把 Release C ABI 文件覆盖进 Debug 输出；私有引用现在排除 `native;build;buildMultitargeting;buildTransitive`，由私有项目按选定 VTK build ID 复制完整共享 runtime。
- Debug 和 Release 下，.NET 8 与 .NET Framework 4.8 的私有解决方案均构建通过；每个输出均有 99 个文件、27 个入口、0 个哈希不匹配，所有 native 资产位于 `native/VtkSharp/win-x64/`，输出根目录无 native DLL。私有测试在 Debug、Release 各 36 项通过。
- WPF smoke 在 Debug/Release × 两托管框架四种组合中各运行一次通过；每次观测 56 个实际加载的 native 模块，其路径与合并清单匹配。smoke 执行初始化、render target reset、控件隐藏/显示、卸载/重载，并在同一进程中关闭窗口后重建新窗口再次渲染。

阶段五的实际 GPU 设备丢失恢复尚未模拟；WPF smoke 的 render target reset 不等同于显卡驱动重置或 D3D device 被系统移除。

### 阶段六：完整验收与文档更新（2026-10-04）

- 公开生成器测试：247 项通过。`generate-bindings --check --incremental` 显示输出最新，复用 205 个类并检查 8 个类。测试构建保留 4 条现有 `CS8604` 与 1 条 `MSTEST0032` 分析警告。
- 新 NuGet 包在 .NET 8 与 .NET Framework 4.8 独立消费应用中运行 Cone（输出 7 个点）及 XML UnstructuredGrid reader（读回 4 个点）。XML 场景还枚举进程模块，确认 `VtkSharp.Native.IOXML.dll` 及其 VTK 闭包都从应用 `native/VtkSharp/win-x64/` 加载。
- 两种框架的 publish 目录复制到新位置后，在清除 `VTK*` 环境变量、将 PATH 限定为 Windows System32 的进程中再次运行 XML 场景成功；实际加载路径随目录搬迁改变到新应用目录，无 NuGet 缓存或开发安装依赖。
- 构建验证用临时 fixture 覆盖两个失败诊断：缺少入口依赖时报告 `Unresolved application dependency`；同名不同哈希的 `vtkCommonCore` 被报告冲突，未复制或覆盖。
- 性能抽样比较公开旧包 `26.1004.238`（73 个 runtime 文件、单 C ABI 入口）和新包 `26.1004.905`（98 个 runtime 文件、26 个入口），均使用同一 VTK Release build id。在 .NET 8 新进程中测量 Cone 首次 `New/Update`，各舍弃 1 次预热并保留 10 次，运行时清空 `VTK*` 并将 PATH 限定为 System32。中位首次使用时间由 16.44 ms 降至 11.15 ms，调用后工作集由 38,203,392 降至 30,502,912 字节；进程 native 模块由 73 个降至 27 个。该结果仅适用于该机器、.NET 8 Release 和 Cone 场景，包含首次 JIT 影响，不能代表所有模块或 CAD 宿主启动收益。
- 验收文档已同步公开生成器、私有构建脚本、架构说明及 WPF runtime 布局。验收时公开验证包与消费应用只存放在本地 ignored `artifacts/`，未发布。

阶段六完成了当前公开 NuGet + 私有工程集成的自动验收。真实 CAD 宿主中的同名异内容 DLL 冲突、实际 D3D device lost 恢复和独立私有 NuGet 包的包级最终 manifest 与部署规则留待具备目标环境/交付形态后验证。
