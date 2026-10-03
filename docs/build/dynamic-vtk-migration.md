# 动态 VTK 迁移结果与约束

决策日期：2026-10-04。状态：Release/Debug 迁移已完成；私有 .NET 8 Debug WPF smoke 的访问冲突已定位并修复，见“验证结果”。

本文从迁移计划整理为当前实现说明。公开构建细节见 [VTK 构建](vtk.md) 和 [VtkSharp 构建](vtksharp.md)；私有构建与 WPF 约束见私有仓库 `README.md`、`docs/architecture.md` 和 `docs/wpf.md`。

## 当前架构

公开托管包装器调用单个 `VtkSharp.Native.dll`。私有托管包装器通过固定版本 NuGet 包消费公开 VtkSharp，同时调用独立的 `BRDI.VtkSharp.Native.dll`。两个 native 入口链接到同一份匹配配置的动态 VTK，部署时将两入口依赖闭包合并到应用输出目录。

```text
VtkSharp.dll ----------------------> VtkSharp.Native.dll --+
                                                           +--> Shared VTK DLLs
BRDI.VtkSharp.dll / BRDI.VtkSharp.Wpf.dll                 |
                 -----------------> BRDI.VtkSharp.Native.dll+
```

- VTK 固定为 9.7.0；Release 和 Debug 分别安装到独立目录，使用动态 MSVC CRT（`/MD`、`/MDd`）。
- VtkSharp 默认动态链接；NuGet `26.1004.236` 只交付 Release runtime。私有项目用该包提供托管 API，以匹配所选 VTK 安装构建私有 native。
- 依赖收集器递归检查普通导入与延迟导入，并显式包含 OpenGL2 渲染模块。输出 `native-dependencies.json`，记录入口、来源、散列、配置、VTK build id、依赖边及系统/CRT 依赖。复制前验证散列；缺失依赖或同名不同内容文件会使构建失败。
- 运行文件平铺复制到应用输出目录；Windows 系统文件和 MSVC CRT 不由 VTK 依赖复制器携带。许可文件随公开 NuGet 和私有聚合输出提供。
- 清单与 DLL 按配置隔离：native 输出包含配置名，公开动态汇总输出位于 `artifacts/bin/dynamic/<Configuration>/<TFM>`，私有独立输出位于 `artifacts/bin/standalone-dynamic/<Configuration>/<TFM>`。禁止 Release 和 Debug 文件互相覆盖。
- 首阶段私有聚合 Release 构建曾验证通过；最终架构已将私有 native 拆分为 `BRDI.VtkSharp.Native.dll`，正式构建不再要求公开源码 submodule。

## 构建入口

VTK 共享库使用：

```powershell
pwsh tools/build-vtk-for-vtksharp.ps1 -Action All -Configuration Release -Linkage Shared
pwsh tools/build-vtk-for-vtksharp.ps1 -Action All -Configuration Debug -Linkage Shared
```

公开 native 和验证使用独立配置：

```powershell
pwsh tools/build-native.ps1 -Configuration Release -Linkage Dynamic -VtkDir <Release VTK_DIR>
pwsh tools/build-native.ps1 -Configuration Debug -Linkage Dynamic -VtkDir <Debug VTK_DIR>
pwsh tools/verify-workflow.ps1 -Mode Final -RuntimeMode Isolated -Linkage Dynamic -Configuration Release -VtkDir <Release VTK_DIR> -Example GeometricObjects/Cone
```

NuGet 打包使用新的、未占用版本号：

```powershell
pwsh tools/package-nuget.ps1 -Version <new-version> -VtkDir <Release VTK_DIR>
```

私有构建应使用本地 NuGet 源和对应配置的公开 native runtime 目录：

```powershell
pwsh tools/build-all.ps1 -Configuration Release -Linkage Dynamic -VtkDir <Release VTK_DIR> -VtkSharpPackageRuntimeDirectory <matching public runtime> -VtkSharpPackageSourceDirectory <local NuGet source> -VtkSharpPackageVersion 26.1004.236
```

`VtkDir` 和公开 runtime 清单中的 VTK build id、配置与架构必须匹配。不要从机器 `PATH` 解析依赖，也不要在隔离验证中把 VTK 开发目录加入 `PATH`。

## 验证结果

验证环境为 Windows x64、Visual Studio 2026/MSVC、VTK 9.7.0。静态安装与基线产物保留未覆盖。原始工作区提交与构建证据见 `artifacts/verification/dynamic-vtk-migration/`。

| 检查 | 结果 |
| --- | --- |
| VTK Shared Release/Debug 构建与安装 | 通过；Debug 显式 OpenGL 模块名按 `d.dll` 配置后缀解析 |
| 公开 Release/Debug native 动态构建 | 通过；依赖清单各含 73 个运行文件 |
| 公开生成器测试 / managed 测试 | Release 生成器 235 项、managed 40 项通过；Debug managed 40 项通过 |
| 公开隔离 Release/Debug Cone 示例 | 通过；部署目录独立运行，未依赖开发 VTK 路径 |
| Release NuGet 包 | `26.1004.236`，24,753,760 字节；73 个 DLL 与许可文件 |
| NuGet 消费 | .NET 8 和 .NET Framework 4.8 均在清空 VTK 环境变量及相关 `PATH` 后构建、运行；加载 VTK 模块均来自应用部署目录 |
| 私有独立 Release/Debug 构建 | 通过；两个入口合并后分别为 74 个运行文件 |
| 私有测试 | Release 36 项、Debug 36 项通过 |
| 私有 WPF Release smoke | .NET 8 与 .NET Framework 4.8 均通过 |
| 私有 WPF Debug smoke | .NET 8 与 .NET Framework 4.8 均通过；修复后每框架连续运行 3 次，检查实际加载的 74 个 native 模块 |

VTK 上游 Debug 编译有大量弃用和数值转换警告，但完整构建、安装及两套 native 链接通过。公开回调测试现有 MSTest 分析器警告（MSTEST0032）；本次迁移未改动这些断言。

私有 .NET 8 Debug WPF smoke 的 `coreclr.dll / 0xC0000005` 根因是 native 配置混用。Crash dump 显示首次 `AttachCursorObserver → vtkObject.AddObserverCore → vtkObject_AddObserverCallback` 调用时崩溃：私有入口从应用根目录加载 Debug VTK，公开入口却由 `.deps.json` 指向 NuGet 的 `runtimes/win-x64/native/VtkSharp.Native.dll`，并加载 Release VTK。仅把 Debug DLL 复制到应用根目录不能覆盖 .NET 的 NuGet native 解析路径，跨配置传递 VTK 对象违反 ABI 约束。

修复在私有项目的两个 `VtkSharp` PackageReference 上设置 `ExcludeAssets="native"`，由匹配配置的合并 runtime 提供两个 native 入口与全部依赖。普通构建默认选择私有 native 输出目录，也必须存在完整清单和两个入口。未修改 CLR、VTK、回调或 WPF 渲染实现。复验 Debug/Release × .NET 8/.NET Framework 4.8，每项连续 3 次 smoke 通过；每次检查 74 个 native 模块均来自应用根目录，文件 SHA-256 匹配清单。Debug/Release 私有测试各 36 项通过。复验工具为私有仓库 `tools/verify-wpf-smoke.ps1`；dump 分析、加载清单和 TRX 位于 `artifacts/verification/wpf-debug-fix/`。原迁移目录保留修复前结果，供追溯。

私有 .NET 8 Debug 另在新建的 `dotnet publish` 目录复验通过；启动时清空 `VTK_DIR`、`VTK_ROOT`，`PATH` 仅保留 Windows 目录，74 个 native 模块的实际路径和哈希仍匹配部署清单。未进行另一台干净机器或虚拟机复验，也未完成对实际用户环境的手动旋转/缩放验收。自动 Cone smoke 验证了渲染输出及隔离启动；截图和进程加载清单保存在 `artifacts/verification/dynamic-vtk-migration/`。

## ABI 与部署约束

- 动态链接不改变 VTK 引用计数、native 所有权、回调委托保活、字符串编码和跨封装传递的既有约定。
- 私有扩展和公开封装必须使用同一 VTK 源码版本、构建标识、架构、配置和兼容工具链。文件名相同不证明 ABI 兼容。
- VTK 对象工厂依赖 `vtk_module_autoinit` 及显式 OpenGL2 模块；不能假设扫描入口导入表即可覆盖所有运行时模块。
- 同一进程中的其他插件若加载不兼容的同名 VTK DLL，当前平铺部署不能隔离这些模块；CAD 宿主集成需单独验证加载冲突。
- 当前 NuGet 包仅提供 Windows x64 Release 运行资产；Debug 用本地构建。NuGet 版本不得原位覆盖，修订后使用新版本号。
