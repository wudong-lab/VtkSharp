# VtkSharp 目录结构迁移设计

本文记录 VtkSharp 项目目录结构调整方案。目标是让源码布局与后续 NuGet 拆包、WPF 扩展模块、native 组件拆分、生成器维护和示例验证的职责边界一致。

## 背景

当前仓库已经具备几个相对独立的子系统：

- `src/bindings/VtkSharp`：核心 C# 绑定项目。
- `src/bindings/VtkSharp.Wpf`：WPF 扩展项目。
- `src/native`：native C ABI shim，目前同时包含核心绑定导出和 WPF OpenGL/D3D interop 代码。
- `generator`：绑定代码生成器、配置、schema、白名单和生成器测试。
- `examples/ExampleBrowser`：WPF 示例浏览器和手动验证入口。

现有结构已经能分别打包 `VtkSharp` 和 `VtkSharp.Wpf`，但目录边界还没有完全反映长期维护目标。尤其是 generator、examples、bindings、native 代码的职责混在不同层级，后续拆 native 包、组织 CI、维护示例和发布 NuGet 时容易产生路径和语义混淆。

## 目标

目录迁移后的结构应满足以下目标：

- 核心绑定、WPF 扩展、native shim、WPF native interop 按发布和职责边界分开。
- generator 拥有独立 solution，作为开发期工具链维护。
- examples 拥有独立 solution，作为示例开发和手动验证入口。
- 测试项目跟随所属子系统，不在仓库根目录额外建立统一 `tests` 目录。
- 迁移过程尽量分阶段进行，避免一次性同时调整目录、项目引用、native DLL 边界和打包逻辑。

## 目标目录结构

```text
VtkSharp/
├── src/
│   ├── bindings/
│   │   ├── VtkSharp.slnx
│   │   ├── Directory.Build.props
│   │   ├── VtkSharp/
│   │   │   ├── VtkSharp.csproj
│   │   │   └── ...
│   │   ├── VtkSharp.Native/
│   │   │   ├── CMakeLists.txt
│   │   │   ├── CMakePresets.json
│   │   │   ├── include/
│   │   │   └── src/
│   │   ├── VtkSharp.Wpf/
│   │   │   ├── VtkSharp.Wpf.csproj
│   │   │   └── ...
│   │   ├── VtkSharp.Wpf.Native/
│   │   │   ├── CMakeLists.txt
│   │   │   ├── include/
│   │   │   └── src/
│   │   └── VtkSharp.Tests/
│   │       ├── VtkSharp.Tests.csproj
│   │       └── ...
│   ├── generator/
│   │   ├── VtkSharp.Generator.slnx
│   │   ├── Directory.Build.props
│   │   ├── config/
│   │   ├── schemas/
│   │   ├── whitelist/
│   │   ├── lib/
│   │   ├── VtkSharp.Generator.Core/
│   │   │   ├── VtkSharp.Generator.Core.csproj
│   │   │   └── ...
│   │   ├── VtkSharp.Generator.Cli/
│   │   │   ├── VtkSharp.Generator.Cli.csproj
│   │   │   └── ...
│   │   └── VtkSharp.Generator.Tests/
│   │       ├── VtkSharp.Generator.Tests.csproj
│   │       ├── TestData/
│   │       └── ...
│   └── examples/
│       ├── VtkSharp.Examples.slnx
│       ├── README.md
│       └── ExampleBrowser/
│           ├── ExampleBrowser.csproj
│           ├── App.xaml
│           ├── MainWindow.xaml
│           ├── Examples/
│           └── ExtraExamples/
├── docs/
│   ├── workflow/
│   ├── learning/
│   └── superpowers/
├── tools/
│   ├── build-native.ps1
│   ├── package-nuget.ps1
│   └── ...
├── .claude/
├── .vscode/
├── .gitignore
├── AGENTS.md
├── CLAUDE.md
├── LICENSE
└── README.md
```

## 目录边界

### `src/bindings`

`src/bindings` 是运行时绑定和 NuGet 包的主目录。它包含核心 C# wrapper、WPF 扩展、native shim、WPF native interop 和绑定层测试。

`VtkSharp.slnx` 应用于日常开发核心绑定、WPF 扩展、native 相关项目和绑定层测试。`ExampleBrowser` 可以同时出现在 examples solution 中，但不应成为 bindings solution 的必需组成部分。需要调试库改动时，可以按需要把示例项目加入 bindings solution。

### `src/generator`

`src/generator` 是代码生成器子系统。它包含 generator solution、生成器核心库、CLI、测试、白名单、schema、配置和本地第三方库。

generator 是开发期工具链，不是最终运行时包。它应该有独立的 `VtkSharp.Generator.slnx`，避免构建或打开生成器时误加载 WPF、native 示例和运行时包项目。

### `src/examples`

`src/examples` 是示例和手动验证入口。它包含 `VtkSharp.Examples.slnx` 和 `ExampleBrowser`。

示例项目可以引用源码项目，也可以在后续改为引用本地打包产物。目录结构不强制绑定其中一种方式。开发期优先使用项目引用，方便调试核心库和 WPF 扩展。

### 测试项目

不建立根目录 `tests`。测试跟随子系统放置：

- `src/bindings/VtkSharp.Tests`：核心 wrapper、P/Invoke、native export、事件、生命周期和 WPF interop 边界测试。
- `src/generator/VtkSharp.Generator.Tests`：生成器配置、白名单、类型映射、输出比较和测试数据。

如果后续出现跨子系统的大型集成测试，再单独评估是否建立 `src/integration-tests` 或根目录 `tests`。

## Native 拆分策略

目录结构上预留两个 native 项目：

- `VtkSharp.Native`：核心 VTK C ABI shim。
- `VtkSharp.Wpf.Native`：WPF OpenGL/D3D interop 和 D3DImage 相关 native 代码。

实现迁移时建议分阶段处理：

第一阶段只迁移目录和项目路径，暂时保持现有单 native DLL 策略。也就是说，核心 native 和 WPF native 代码可以先都编译进 `VtkSharp.Native.dll`，以降低迁移风险。

第二阶段再拆成两个 native DLL：

- `VtkSharp.Native.dll`
- `VtkSharp.Wpf.Native.dll`

拆分 native DLL 时需要重新设计 CMake target、P/Invoke library name、NuGet runtime asset、DLL 搜索路径和 native 对象跨 DLL 边界规则。

## NuGet 包边界

长期目标是两个主要 NuGet 包：

- `VtkSharp`：核心托管绑定，依赖或携带核心 native runtime asset。
- `VtkSharp.Wpf`：WPF 扩展包，依赖 `VtkSharp`，并在 native 拆分完成后携带 WPF native runtime asset。

`VtkSharp` 的 public API 不应引用 WPF、WinForms、DevExpress 或其他 UI 框架类型。`VtkSharp.Wpf` 可以引用 `VtkSharp`，但反向依赖不允许出现。

## 迁移步骤

### 阶段一：纯目录迁移

1. 将 `generator` 移到 `src/generator`。
2. 将 `examples` 移到 `src/examples`。
3. 将 `src/native` 移到 `src/bindings/VtkSharp.Native`。
4. 保留或移动当前 `src/bindings/VtkSharp`、`src/bindings/VtkSharp.Wpf`、`src/bindings/VtkSharp.Tests` 到目标位置。
5. 更新 `.slnx` 中的项目路径。
6. 更新 `.csproj` 中的 `ProjectReference`、native DLL 路径和 README 打包路径。
7. 更新 `tools/build-native.ps1`、`tools/package-nuget.ps1` 中的路径。
8. 更新测试中硬编码的仓库相对路径。
9. 更新 README、AGENTS.md 或相关开发文档中的目录说明。

阶段一不改变程序集名、包名、命名空间、native DLL 名称和 public API。

### 阶段二：solution 边界整理

1. `src/bindings/VtkSharp.slnx` 包含核心绑定、WPF 扩展、native 项目和绑定层测试。
2. `src/generator/VtkSharp.Generator.slnx` 包含生成器核心库、CLI 和生成器测试。
3. `src/examples/VtkSharp.Examples.slnx` 包含示例项目。
4. 如需全量 CI 构建，可以后续再添加根目录聚合 solution；初始迁移不强制添加。

### 阶段三：WPF native 拆分

1. 从 `VtkSharp.Native` 中分离 WPF native 源码到 `VtkSharp.Wpf.Native`。
2. 为 `VtkSharp.Wpf.Native` 创建独立 CMake target。
3. 将 WPF 托管层 P/Invoke 指向新的 native library name。
4. 在 `VtkSharp.Wpf` NuGet 包中携带 `VtkSharp.Wpf.Native.dll`。
5. 验证 `VtkSharp.Wpf.Native.dll` 和 `VtkSharp.Native.dll` 使用一致的 VTK、CRT、平台和配置。

阶段三是独立任务，不建议和阶段一混在同一次大改中完成。

## 风险与应对

### 路径硬编码

当前测试、脚本、项目文件和文档中存在仓库相对路径。迁移时容易遗漏。

应对方式：使用 `rg` 搜索旧路径片段，例如 `src\\native`、`src/native`、`examples\\ExampleBrowser`、`generator\\`、`src\\bindings`，逐项更新并运行构建验证。

### Native DLL 加载

`VtkSharp` 和 `VtkSharp.Wpf` 当前都通过 `InteropInfo.NativeLibraryName` 指向 `VtkSharp.Native`。目录迁移不应改变该行为。

应对方式：阶段一保持 native DLL 名称不变，先验证现有 P/Invoke 能正常加载。native 拆分放到后续阶段单独处理。

### WPF native 拆分后的对象边界

WPF native interop 会返回 `vtkRenderWindow`、`vtkRenderer` 等 native 指针给 C# wrapper。拆成两个 DLL 后，需要确保这些对象来自同一套 VTK runtime，并且生命周期语义没有跨模块冲突。

应对方式：拆分前补充最小生命周期验证，包括创建、渲染、窗口卸载、重复加载、Dispose 顺序和进程退出。

### NuGet runtime asset

native DLL 路径变化会影响 `runtimes/win-x64/native` 打包。遗漏 runtime asset 会导致消费项目运行时 `DllNotFoundException` 或入口点找不到。

应对方式：每次打包后检查 `.nupkg` 内容，并用一个最小消费项目验证安装包。

### CI 与本地开发命令

目录迁移会改变常用命令路径，例如 `dotnet build`、`dotnet test`、`cmake --preset`、打包脚本路径。

应对方式：迁移完成后在 README 或开发文档中更新推荐命令，并保留 `tools` 下脚本作为统一入口。

## 验证计划

阶段一完成后至少运行：

```powershell
dotnet build src/bindings/VtkSharp.slnx
dotnet test src/bindings/VtkSharp.slnx
dotnet build src/generator/VtkSharp.Generator.slnx
dotnet test src/generator/VtkSharp.Generator.slnx
dotnet build src/examples/VtkSharp.Examples.slnx
```

如果本机 VTK 和 CMake 环境可用，还应运行：

```powershell
tools/build-native.ps1 -Configuration Debug
tools/package-nuget.ps1 -Configuration Release
```

打包验证应检查：

- `VtkSharp` 包含核心 managed assembly 和 `VtkSharp.Native.dll`。
- `VtkSharp.Wpf` 依赖 `VtkSharp`。
- native 拆分前，`VtkSharp.Wpf` 不单独携带 WPF native DLL。
- native 拆分后，`VtkSharp.Wpf` 携带 `VtkSharp.Wpf.Native.dll`。

WPF 手动验证应运行 `ExampleBrowser`，至少检查：

- 普通 VTK 示例能正常渲染。
- `VtkRenderHost` 示例能打开、关闭和重复打开。
- `VtkOpenGlD3DImageRenderControl` 示例能显示、缩放、响应鼠标键盘输入。
- 生命周期压力示例关闭窗口后没有明显 native 崩溃或资源释放顺序问题。

## 非目标

本次目录迁移设计不要求：

- 改变 public API。
- 重命名 NuGet 包。
- 重命名 C# namespace。
- 立即拆分 native DLL。
- 重写 generator 配置格式。
- 大规模重构示例代码。

## 推荐实施顺序

推荐先执行阶段一和阶段二，确保目录和 solution 边界稳定。确认构建、测试、示例和打包脚本恢复后，再单独设计并实施阶段三的 native DLL 拆分。

这样可以把“文件路径迁移风险”和“native ABI/运行时加载风险”分开处理，降低一次性变更的排查难度。
