# 构建与打包 VtkSharp

VtkSharp 包括托管 `VtkSharp.dll` 与链接静态 VTK 的 `VtkSharp.Native.dll`。
当前构建和 NuGet native 资产面向 Windows x64，不承诺其他平台可用。

## 前置条件与路径

先按 [VTK 构建说明](vtk.md) 安装 VTK 9.7.0，再按 [README](../../README.md#2-设置-vtk-环境变量)
设置 `VTK_ROOT`（生成器）与 `VTK_DIR`（CMake package）。二者应指向同一安装。

`build-native.ps1`、`build-all.ps1`、`package-nuget.ps1`、`verify-workflow.ps1`
默认读取 `VTK_DIR`，显式 `-VtkDir` 参数优先；构建 native 时会将其写入 CMake 配置。
未设置时会提示配置路径，不依赖另一台机器的目录或隐式 CMake cache。

native 脚本优先使用 VS 2026，仅在对应 generator/实例不可用时尝试 VS 2022。
此回退不保证已有 VTK 安装与另一套工具链兼容，也不适用于 VTK 构建脚本。
推荐使用同一工具链构建 VTK 与 native DLL。

## 配置与 CRT 匹配

| 仓库构建配置 | Native 配置 | VTK 静态库配置 | Native MSVC CRT |
| --- | --- | --- | --- |
| `Debug` | `Debug` | `Debug` | `/MDd` |
| `Release` | `Release` | `Release` | `/MD` |

CRT 匹配是 native DLL 与 VTK 静态库之间的要求。托管程序集本身不使用 MSVC CRT；
上述托管配置一致是本仓库按 `$(Configuration)` 选取、复制 native DLL 的约定，
不是 P/Invoke 禁止托管 Debug 调用 Release DLL。不要混用 ABI 或版本不匹配的产物。

## 构建与运行

在仓库根目录执行：

```powershell
.\tools\build-all.ps1 -Configuration Release
dotnet run --project src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release
```

`build-all.ps1` 和 `build-native.ps1` 保留默认 Debug 配置；文档始终显式选择 Release。
一键脚本只构建绑定库及 native 项目，不运行生成器、测试或示例。
每次运行重新创建 `artifacts/bin`，收集 `netstandard2.0`、`net8.0` 的
`VtkSharp.dll`、PDB、XML API 文档及 native DLL。该目录是构建产物，不应存放手工文件。

也可以分步构建：

```powershell
.\tools\build-native.ps1 -Configuration Release
dotnet build src/bindings/VtkSharp.slnx --configuration Release
```

Debug 时先安装 Debug VTK，再将所有仓库构建/示例命令改为 Debug。
`-SkipNativeBuild` 仅用于已有匹配 native DLL 的情况，不会生成缺失 DLL。
`dotnet build` 不会自动编译 C++；native DLL 缺失时托管编译仍可能通过，但运行会失败。

## 本地 NuGet 打包

```powershell
.\tools\package-nuget.ps1 -Configuration Release
```

脚本先构建 native，再打包到 `artifacts/nuget/<version>`，包含两个 TFM 的托管库、XML 文档、
`runtimes/win-x64/native/VtkSharp.Native.dll`、包 README 和 VTK 许可文件。
版本号按本地构建时间生成，规则见 `src/bindings/Directory.Build.props`，不是语义化版本兼容性承诺。
打包会检查 native DLL 存在，避免生成缺失 native 资产却看似可安装的包。

脚本只创建本地包，不发布到 NuGet.org；可将输出目录添加为自己的 NuGet 源。
消费应用必须运行于 x64，且托管库与 native DLL 来自配套构建。
不同 .NET 宿主的 native 资产探测方式可能不同，尤其是 .NET Framework 应用，应检查输出目录。

## 部署与常见问题

- `DllNotFoundException`：检查 `VtkSharp.Native.dll` 是否复制到应用可搜索的位置，以及其依赖能否加载。
- `BadImageFormatException`：先检查应用是否以 x86 运行，或混用了不同架构的 native DLL。
- Release native DLL 使用动态 MSVC CRT；目标机器需要匹配的 x64 Visual C++ 运行库。静态链接 VTK 不代表完全没有系统/native 依赖。
- Debug 使用开发工具链中的调试 CRT，不作为普通用户的分发配置。
- `artifacts/bin` 是库产物集合，不是自动生成的应用安装包。分发前检查实际 native 依赖和相关第三方许可声明。
- 示例需要 .NET 8 Desktop Runtime 和可用图形环境；无显示设备运行、跨 GPU 图像一致性不在当前保证范围内。

## 验证

```powershell
.\tools\verify-workflow.ps1 -Example GeometricObjects/Cone
```

需要 .NET 10 SDK、.NET 8 SDK/运行时和完整工具链。仅在白名单变化需要更新生成文件时加
`-Regenerate`。报告与人工验收边界见 [统一验证入口](../workflow/verification.md)。
