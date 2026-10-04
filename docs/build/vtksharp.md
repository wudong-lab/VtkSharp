# 构建与打包 VtkSharp

VtkSharp 由 managed `VtkSharp.dll`、模块化公开 C ABI `VtkSharp.Native.<Group>.dll` 和运行时依赖组成。当前默认使用 Windows x64、VTK 9.7.0 Shared、Release `/MD`；静态构建仍可显式选择作为基线或回退路径。

## 环境和构建

先按 [VTK 构建说明](vtk.md) 在独立目录构建、安装 VTK。动态 Debug/Release 分别安装至 `install/Debug` 和 `install/Release`；不得复用静态缓存或把不同配置 DLL 混放。

`VTK_DIR` 指向对应配置下的 `lib/cmake/vtk-9.7`；`VTK_ROOT` 是同一安装根目录。native CMake、头文件和运行时必须来自匹配的 VTK build。构建优先使用 VS 2026，必要时可回退 VS 2022，但需保证和 VTK 工具链兼容。

```powershell
$vtkDir = "D:\Code\VTK\VtkGitBuild-shared\install\Release\lib\cmake\vtk-9.7"

# Build the public native wrapper and its exact dependency closure.
.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir

# Build managed libraries and collect the full runtime set.
.\tools\build-all.ps1 -Configuration Release -VtkDir $vtkDir
```

一键构建会保留本机原有 artifacts 内容，只按旧清单和哈希清理由构建工具管理的过期 DLL。native 依赖扫描普通导入、延迟导入和显式运行模块，递归生成 `native-dependencies.json`；缺失文件、同名异内容冲突和失效哈希会阻止构建。Windows 系统 DLL 与 MSVC CRT 要求单独记录，不从系统目录复制。

## 本地 NuGet 包

```powershell
$newVersion = "27.1004.1" # Choose a version not present in the local package source.
.\tools\package-nuget.ps1 -Configuration Release `
    -Version $newVersion `
    -VtkDir D:\Code\VTK\VtkGitBuild-shared\install\Release\lib\cmake\vtk-9.7
```

打包只接受 Release、Dynamic；可用 `-Version` 显式指定未使用的 `x.y.z` 版本，不传时根据本地时间生成版本。已存在同版本包会失败，不覆盖包。包把 managed assemblies 放入相应 TFM，将完整可分发 DLL 闭包放在 `native/VtkSharp/win-x64/`，并携带依赖清单、VTK 版权声明和第三方许可文件。为避免 NuGet 对无扩展名 `LICENSE` 项生成过长路径，打包时将其重命名为 `.txt`，映射记录在 `licenses/VTK/license-file-renames.json`。脚本只生成本地 NuGet 包，不发布到 NuGet.org。

消费项目通过固定版本 `PackageReference` 使用本地源，并确保消费进程为 Windows x64。包内 `buildTransitive/VtkSharp.targets` 将 native 资产复制到 build 和 publish 输出的 `native/VtkSharp/win-x64/` 子目录，加载器默认从这里按需加载入口及依赖；首次调用前可通过 `VtkSharpRuntime.ConfigureNativeRuntimeDirectory` 指定其他 runtime 目录。Release 机器需要兼容的 x64 Visual C++ 运行库。

## 验证与部署隔离

```powershell
.\tools\verify-workflow.ps1 -Configuration Release `
    -Linkage Dynamic -RuntimeMode Isolated `
    -VtkDir D:\Code\VTK\VtkGitBuild-shared\install\Release\lib\cmake\vtk-9.7 `
    -Example GeometricObjects/Cone
```

隔离验证在新进程中运行发布产物，不添加 VTK 开发目录到 `PATH`，并清空 `VTK_ROOT`、`VTK_DIR`。实际加载的 VTK DLL 应全部来自部署目录。图形示例还需要 .NET 8 Desktop Runtime 和可用图形设备。

目前验证通过：VTK 9.7.0 Shared Release/Debug、公开 native 与隔离 Cone smoke、每配置 40 个公开 managed 测试、Release NuGet 打包，以及 .NET 8/.NET Framework 4.8 固定版本包消费。私有 standalone Release/Debug 构建及每配置 36 个测试通过。私有 .NET 8 Debug WPF 的访问冲突已修复：排除公开 NuGet 的 native 资产，避免与本地 Debug VTK 混用。两配置、两框架 WPF smoke 均连续 3 次通过。

静态 fallback 可使用 `-Linkage Static`，但不用于 NuGet 包；NuGet 发布脚本只接受 Release Dynamic。动态构建汇总输出位于 `artifacts/bin/<Configuration>/<TFM>`，静态 fallback 输出位于 `artifacts/bin/<TFM>`。这些目录是库文件集合，不是应用安装包，分发前应核对目标 RID、运行库和许可声明。
