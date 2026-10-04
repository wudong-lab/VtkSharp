# 模块化 native 导出层与按需加载设计

记录日期：2026-10-04。状态：设计决策及实施范围已确认，尚未实施。本文记录设计决策、证据、原型验证事项和实施步骤，不代表现有版本已经支持模块化加载或子目录部署。

当前动态链接架构与既有验证结果见 [动态 VTK 迁移结果与约束](dynamic-vtk-migration.md)。

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

用户已同意其余推荐方案。首阶段按下表执行；准确成员、配置 API 和加载实现根据元数据分析及原型结果确定。

| 事项 | 已确认方案 | 实施边界与后续工作 |
| --- | --- | --- |
| 首版合并集合 | 普通模块独立，仅合并经分析确定的渲染初始化组，暂不合并整个 Common Kit | 沿用当前 OpenGL2 后端；准确成员由元数据、私有 WPF 后端和原型验证确定 |
| 消费支持范围 | 首阶段验证 Windows x64、.NET 8 和 .NET Framework 4.8 的 SDK 风格 `PackageReference` 项目 | 非 SDK 项目、`packages.config` 和其他架构暂不纳入首阶段 |
| runtime 定位 | 普通应用以 `AppContext.BaseDirectory` 为基准，允许首次加载前指定 runtime 根目录 | 配置 API 在实施时确定；CAD 宿主通过显式配置定位，加载冲突另行验证 |
| 交付范围 | 首阶段交付完整 runtime，由加载器按需加载 | 暂不按 VTK 功能模块拆分 NuGet 包或交付裁剪工具；后续独立私有包遵循第 6 节约定 |
| 失败与卸载 | 打包验证完整闭包；运行时检查当前请求组，失败报告模块、路径和原因；已加载模块保留到进程退出 | 暂不支持动态卸载和加载后切换 runtime |

加载路径、P/Invoke 绑定方式和工厂初始化入口还需要原型验证，不预先承诺未经验证的实现细节。

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

对 `artifacts/package-runtime/26.1004.238/native-dependencies.json` 按 `ordinary-import` 和 `application-file` 边计算闭包，得到：

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

.NET 8 可在首次 P/Invoke 前注册 assembly 级 resolver，返回按绝对路径加载并缓存的句柄。.NET Framework 4.8 不具备同一 resolver API，需验证调用前预加载与 P/Invoke 的绑定行为；如不满足要求，再决定替代路径。依据见 [.NET native 加载说明](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading)。

首次加载和注册需要同步，避免公开、私有入口并发初始化时重复或遗漏注册。不能把新增加载逻辑放入 `DllMain`。需检查并约束现有 autoinit 的执行时机，不能将“文件已加载”直接视为“所有工厂已按预期注册”。正常调用复用模块句柄和已解析函数地址。

公开和私有入口必须共享 VTK 版本、build id、架构、配置和兼容 CRT；继续保留对象引用计数、指针归属和回调保活约定。子目录不能解决 CAD 宿主中已加载同名异内容 DLL 的冲突，需要单独验证。

## 7. 分阶段实施步骤

### 阶段一：基线核对与规则冻结

- 重新读取两个仓库当前实现和工作区变更；核对私有 C ABI、WPF 后端、通用手写导出和模块依赖。
- 提取配置匹配的 VTK 元数据，记录 Kit、工厂关系和 PE 依赖证据，形成首版分组策略及成员清单。
- 按已确认范围确定输出命名、runtime 配置契约、清单 schema 和跨仓库初始化职责。
- 当前私有 `AGENTS.md` 仍描述聚合到公开 `VtkSharp.Native.dll` 的旧结构，与当前私有架构文档不一致；代码实施时同步修订为私有单入口 `BRDI.VtkSharp.Native.dll` 及共享动态 VTK 的约束。

交付：可审查的策略、依赖证据和实施边界，暂不批量迁移导出代码。

### 阶段二：最小加载原型

- 选一个数据模块、一个计算模块和一个渲染组，构建最小模块入口与子目录部署。
- 验证 .NET 8、.NET Framework 4.8 的首次加载、依赖定位、句柄缓存和渲染工厂行为。
- 加入最小私有调用，验证公开先调用、私有先调用和对象跨入口传递；检查并发首次初始化。
- 每个场景使用新进程，记录实际加载路径、模块集合、耗时和工作集；清除开发 VTK 环境变量及相关 `PATH` 后验证。

交付：原型结果及两套托管运行时的可行调用机制。未通过前不进行全量生成器改造。

### 阶段三：公开生成器与导出层迁移

- 增加策略和元数据模型，YAML DTO 的集合沿用具体可变类型。
- 统一生成模块归属、native targets、C# P/Invoke 库名、组级链接和 autoinit 配置。
- 为手写导出指定归属；移除新 wrapper 对旧单入口的依赖，不生成兼容转发层。
- 覆盖确定性生成和无效策略校验，验证增量生成不会遗留旧目标或覆盖用户手写源码。

交付：公开模块化产物、生成清单和相关验证结果。

### 阶段四：NuGet 与部署迁移

- 调整依赖收集器支持多入口及显式初始化依赖，继续验证文件哈希、build id、缺失项及同名冲突。
- 定义共享部署目录配置、包级资产声明和最终清单生成契约；按目标文件汇总、校验、去重后复制，不依赖包执行顺序。
- 增加自定义包资产和构建规则，使 build、publish 及项目间传递引用都保留指定子目录。
- 使用新 NuGet 版本；验证输出根目录无重复 VTK runtime，加载不依赖 NuGet 缓存或开发安装目录。
- 保留 Release/Debug 输出隔离和许可交付；发布目录可整体复制到另一位置运行。

交付：本地 NuGet 包、隔离消费项目和部署目录检查结果。

### 阶段五：私有扩展与 WPF 迁移

- 保持私有单一 CMake target 和 `BRDI.VtkSharp.Native.dll` 名称，仅按新加载、部署和初始化契约调整 CMake、P/Invoke、WPF 入口及依赖合并工具；不拆分私有导出代码。
- 更新固定公开包版本，确保 Debug 不误用 Release native 资产，两个仓库共享一份 runtime。
- 独立计算私有需求闭包，确定指定公开包未交付的 VTK 和第三方依赖；所有补充文件部署到共享目录并通过构建身份校验。
- 私有入口仅链接其实际需求，避免照搬公开全部模块列表；接受单入口带起其全部必要普通导入依赖，协调渲染初始化职责和调用顺序。
- 验证引用计数、释放及回调，运行 WPF 初始化、resize、关闭重开和 D3DImage 恢复场景。

交付：公开与私有集成产物、WPF 验证结果和同步后的构建文档。

后续独立私有 NuGet 交付：按上述部署契约封装私有 managed/native 资产、补充依赖和独立包级清单，声明公开包版本依赖；验证应用仅引用私有包即可完成还原、构建和发布。该交付属于后续目标，不要求首阶段按功能模块拆分包。

### 阶段六：完整验收与文档更新

- 新进程分别执行数据构造、简单 Filter、XML 读取、Cone 渲染和私有 WPF 场景，按依赖闭包解释加载差异。
- 在 Release/Debug 与两个托管框架的适用组合中检查加载路径、文件哈希和 ABI 一致性。
- 比较相同输入的重复生成结果；改变策略或元数据时，确认仅产生预期差异。
- 验证缺失所需依赖、错误配置和目录冲突的诊断；若采用局部运行检查，验证未使用组缺失的行为。
- 验证私有扩展额外 VTK 模块及其传递依赖的部署和按需加载；后续独立私有包验收需覆盖仅引用私有包、直接同时引用两包、同内容去重及同名异内容失败。
- 检查两个包均不修改对方缓存、不覆盖包级清单、不清空共享目录；最终清单包含每个文件的提供包，实际加载路径和哈希与合并结果一致。
- 测量首次使用延迟和启动收益；确认 WPF 绑定、线程访问和实际渲染效果。
- 更新公开与私有架构、构建说明、生成器文档和协作约束，标记本设计的实施状态。

验收不要求固定的 DLL 数量，而要求加载集合可由当前闭包和初始化需求解释，且公开封装中未使用、非必要的独立组不会被入口全局链接或初始化提前拉起。私有扩展按单一入口的完整需求闭包验收，私有模块分组在公开封装稳定后另立任务讨论。

## 8. 当前验证状态

已完成：公开构建与调用入口阅读、私有架构文档阅读、VTK 官方规则核对、当前安装元数据与 autoinit 核对、指定 Release 清单的三个依赖闭包分析。

尚未执行：模块化构建、子目录 NuGet 消费、两套托管运行时加载原型、跨入口初始化原型、性能比较和模块化后的 WPF 验收。现有动态链接 smoke 结果不能替代上述新方案验证。
