# 构建 VtkSharp 使用的 VTK

当前固定 VTK `9.7.0`，源码基线为 `23f0a095621e91bbdbeace8451e22b950c8e5f46`。新构建默认使用 Windows x64、Visual Studio 2026/2022 和 Shared VTK；原静态 build/install 保留为回退基线。动态 build 与 Debug/Release install 均使用独立目录。

## 工具与源码

需要 Git、PowerShell 7、CMake、Visual Studio 2026 C++ 桌面开发工具和 Windows SDK。VTK 构建脚本固定使用 VS 2026；公开/私有 wrapper 脚本可尝试 VS 2022 回退，但 VTK 与 wrapper 必须使用兼容的工具链。

```powershell
$vtkWorkspace = "D:\Dependencies\VTK"
$vtkSource = Join-Path $vtkWorkspace "source"
$vtkBuild = Join-Path $vtkWorkspace "VtkGitBuild-shared"
git clone --branch v9.7.0 --depth 1 https://gitlab.kitware.com/vtk/vtk.git $vtkSource

# Shared is the default; builds and installs Release.
.\tools\build-vtk-for-vtksharp.ps1 `
    -SourceDirectory $vtkSource `
    -BuildDirectory $vtkBuild

# Build and install the matching Debug configuration separately.
.\tools\build-vtk-for-vtksharp.ps1 -Configuration Debug `
    -SourceDirectory $vtkSource `
    -BuildDirectory $vtkBuild
```

克隆目标应为新目录。已有源码时先核对提交，不覆盖手工修改。

## 目录和产物

- `-Linkage Shared` 为默认值；`-Linkage Static` 仅用于旧基线回退。
- `-BuildDirectory` 默认为 `VtkGitBuild-shared`，静态构建仍使用历史 `VtkGitBuild`。
- `-InstallDirectory` 默认为构建目录下的 `install`。Shared 分配置安装为 `install/Release` 和 `install/Debug`。
- 安装内容包含 CMake package、匹配配置的 VTK DLL/导入库/头文件、hierarchy 文件、`vtk-build-info.json` 和 VTK/第三方许可目录。

```text
VtkGitBuild-shared/
├── CMakeCache.txt             # BUILD_SHARED_LIBS=ON
└── install/
    ├── Release/               # Release DLLs and build record
    └── Debug/                 # Debug DLLs and build record
```

Release native wrapper 需要指向 `install/Release/lib/cmake/vtk-9.7`；Debug 则指向 `install/Debug/lib/cmake/vtk-9.7`。两份安装具有不同 build ID，不得复用同一个 native 依赖清单。

## 分步操作

默认 `-Action All -Configuration Release` 会配置、编译并安装。已有 Shared 配置可单独运行：

```powershell
.\tools\build-vtk-for-vtksharp.ps1 -Action Build -Configuration Debug `
    -BuildDirectory $vtkBuild -InstallDirectory (Join-Path $vtkBuild "install")
.\tools\build-vtk-for-vtksharp.ps1 -Action Install -Configuration Debug `
    -BuildDirectory $vtkBuild -InstallDirectory (Join-Path $vtkBuild "install")
```

Release 使用 `/MD`，Debug 使用 `/MDd`。wrapper、VTK 配置、架构和 toolset 必须匹配。脚本记录源码提交、模块范围、配置和工具链，DLL 文件名相同不代表 ABI 相同。

## 模块范围

配置以 [构建脚本](../../tools/build-vtk-for-vtksharp.ps1) 为准：构建 VtkSharp bindings 使用的公共模块及其传递依赖；禁用不需要的 Qt、MPI、Web、Tk、GPU compute 和语言 wrapper；关闭 VTK 测试、示例与文档构建，保留生成器所需 hierarchy 文件。VTK 安装模块不等于 VtkSharp 已绑定模块，绑定范围仍由白名单决定。

若新增项目必需模块，应同步更新脚本模块清单、安装和 native CMake `find_package`/autoinit 配置，并在新 build ID 下重新构建依赖清单。切换配置、编译器、架构、静态/动态库或 CRT 时使用匹配的独立目录。
