# 构建 VtkSharp 使用的 VTK

当前开发基线为 VTK `v9.7.0`，对应上游提交 `23f0a095621e91bbdbeace8451e22b950c8e5f46`。
默认使用 Windows x64、Visual Studio 2026、静态 VTK 库和动态 MSVC CRT。
VTK 静态库最终链接到 `VtkSharp.Native.dll`；这不表示无需 MSVC 运行库。

## 工具与源码

安装 Git、PowerShell 7、Visual Studio 2026 的 C++ 桌面开发工作负载和 Windows SDK。
CMake 至少使用 4.2，因为 [VS 2026 generator 从该版本加入](https://cmake.org/cmake/help/v4.2/generator/Visual%20Studio%2018%202026.html)。
本脚本不提供 VS 2022 回退。

从 VtkSharp 仓库根目录执行，路径可自行选择：

```powershell
$vtkWorkspace = "D:\Dependencies\VTK"
$vtkSource = Join-Path $vtkWorkspace "source"
$vtkBuild = Join-Path $vtkWorkspace "build"
git clone --branch v9.7.0 --depth 1 https://gitlab.kitware.com/vtk/vtk.git $vtkSource

.\tools\build-vtk-for-vtksharp.ps1 -Configuration Release `
    -SourceDirectory $vtkSource -BuildDirectory $vtkBuild
```

克隆目标应为尚不存在的目录。已有源码时先确认版本，不要直接覆盖本地修改。
脚本检查源码中的版本号为 9.7.0，但不校验 Git 提交；上面的 tag 用于固定可复现的源码基线。

## 目录与参数

- `-SourceDirectory`：VTK 源码目录。
- `-BuildDirectory`：CMake 工程和编译中间文件目录。
- `-InstallDirectory`：安装根目录；省略时为构建目录下的 `install`。
- 未指定源码/构建目录时，脚本保留历史默认值：相对脚本目录的 `../../../VTK/VtkGitSource` 和 `../../../VTK/VtkGitBuild`。公开用户推荐显式传参，不依赖仓库克隆位置。

```text
VTK/
├── source/          # 下载的源码
└── build/           # 构建目录
    └── install/     # 安装根目录（VTK_ROOT）
```

安装步骤不能省略：生成器需要安装后的 headers 和 hierarchy 文件，native 构建需要 CMake package。
安装后按 [README 环境变量说明](../../README.md#2-设置-vtk-环境变量) 设置 `VTK_ROOT` 和 `VTK_DIR`。
这两个变量用于消费已安装的 VTK，不控制本脚本的源码或构建目录。

## Release、Debug 与分步操作

以下命令沿用上文的 `$vtkSource`、`$vtkBuild`：

```powershell
# Configure, build and install both configurations
.\tools\build-vtk-for-vtksharp.ps1 -Configuration Both `
    -SourceDirectory $vtkSource -BuildDirectory $vtkBuild

# Configure only
.\tools\build-vtk-for-vtksharp.ps1 -Action Configure `
    -SourceDirectory $vtkSource -BuildDirectory $vtkBuild

# Build and install Debug after configuration
.\tools\build-vtk-for-vtksharp.ps1 -Action Build -Configuration Debug -BuildDirectory $vtkBuild -SourceDirectory $vtkSource
.\tools\build-vtk-for-vtksharp.ps1 -Action Install -Configuration Debug -BuildDirectory $vtkBuild -SourceDirectory $vtkSource
```

`-Action` 默认为 `All`，`-Configuration` 默认为 `Release`。仅构建 Debug 时也可使用
`-Configuration Debug` 一次完成配置、编译和安装。`-Parallel` 控制并行度；
已有目录切换 generator 时使用新的构建目录，或确认兼容后使用 `-Fresh` 重建 CMake cache。

Debug 和 Release 可安装到同一目录；Debug 库带 `d` 后缀，CMake 分配置引用。
本脚本显式设置动态 MSVC CRT：Release 为 `/MD`，Debug 为 `/MDd`。
native DLL 应链接同配置、匹配工具链与 CRT 的 VTK 静态库。

## 模块范围

实际配置以 [构建脚本](../../tools/build-vtk-for-vtksharp.ps1) 为准，不在文档重复维护全部 CMake 开关。

- `StandAlone`、`Rendering`、`Imaging`、`Views` 模块组设为 `WANT`，常用依赖满足时构建。
- 脚本列出的必需模块设为 `YES`。
- 禁用 Qt、MPI、Web、Tk、CUDA、HIP、Kokkos、WebGPU、remote modules，以及 Python/Java/JavaScript 等语言包装。
- `VTK_ENABLE_WRAPPING=ON` 用于生成 hierarchy 文件，不代表启用其他语言 wrapper。
- 禁用 VTK 测试、示例和文档构建，使用 `STDThread` SMP 实现。

VTK 安装中的模块不等于 VtkSharp 已绑定的模块；后者由正式白名单决定。

## 后续增加模块

例如需要 `IOXML` 时，可在原目录重新配置并增量安装：

```powershell
cmake -S $vtkSource -B $vtkBuild -DVTK_MODULE_ENABLE_VTK_IOXML=YES
cmake --build $vtkBuild --config Release --parallel
cmake --install $vtkBuild --config Release
```

随后通过 [候选白名单流程](../generator.md#白名单变更流程) 补充绑定、生成并验证。
若该模块成为项目必需项，还应同步构建脚本的模块列表，确保新用户可以构建。
切换编译器、架构、静态/动态库或 CRT 时应使用新构建目录，避免混用缓存和二进制。
