# 构建 VtkSharp 使用的 VTK

当前使用 VTK 9.7.0，源码基线为 `23f0a095621e91bbdbeace8451e22b950c8e5f46`。默认 Windows x64、Visual Studio 2026、静态 VTK；Release `/MD`，Debug `/MDd`。

## 构建与安装

需要 Git、PowerShell 7、CMake、Visual Studio C++ 桌面开发工具和 Windows SDK。

```powershell
$vtkWorkspace = "D:\Dependencies\VTK"
$vtkSource = Join-Path $vtkWorkspace "source"
$vtkBuild = Join-Path $vtkWorkspace "VtkGitBuild"
git clone --branch v9.7.0 --depth 1 https://gitlab.kitware.com/vtk/vtk.git $vtkSource
.\tools\build-vtk-for-vtksharp.ps1 -SourceDirectory $vtkSource -BuildDirectory $vtkBuild
.\tools\build-vtk-for-vtksharp.ps1 -Configuration Debug -SourceDirectory $vtkSource -BuildDirectory $vtkBuild
```

克隆目标应为新目录；已有源码时核对提交，不覆盖手工修改。默认 `-Linkage Static -Action All -Configuration Release`，可用 `-Action Configure`、`Build` 或 `Install` 分步执行。静态 build 默认 `VtkGitBuild`，安装默认 `VtkGitBuild/install`；Debug/Release 的库通过安装的 CMake target 配置选择。

```powershell
$env:VTK_ROOT = Join-Path $vtkBuild "install"
$env:VTK_DIR = Join-Path $env:VTK_ROOT "lib\cmake\vtk-9.7"
```

安装包含静态库、头文件、CMake package、hierarchy、构建记录和第三方许可。native 封装与 VTK 必须匹配版本、架构、配置和工具链；静态 VTK 与静态 CRT 是独立选择，本项目保留动态 CRT。

## 模块范围

以 [构建脚本](../../tools/build-vtk-for-vtksharp.ps1) 为准：启用 bindings 所需模块及传递依赖，禁用不需要的 Qt、MPI、Web、Tk、GPU compute 和语言 wrapper；关闭 VTK 测试、示例和文档构建，保留生成器所需 hierarchy。

新增必需模块时，同步更新脚本、安装和 native 查找／链接／autoinit 配置。当前白名单包含此次切换前新增的导出接口；切换链接方式不回退白名单或导出源码。

## 可选动态构建

显式 `-Linkage Shared` 使用独立 `VtkGitBuild-shared` 缓存，Debug/Release 分别安装到 `install/Debug` 和 `install/Release`。对应 wrapper 使用 `-Linkage Dynamic`。不要将 shared 安装路径传给默认静态构建，也不要混用两种缓存或不同配置的 DLL。
