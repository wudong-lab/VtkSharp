# 构建 VtkSharp 使用的 VTK

VtkSharp 当前以 VTK 9.7.0 为开发基线。在 Windows 上使用 Visual Studio 2026、x64 和静态 VTK 库，VTK 最终链接到 `VtkSharp.Native.dll`，避免发布时携带大量 VTK 动态库。

## 目录约定

```text
D:\Code\VTK\VtkGitSource         # VTK v9.7.0 源码
D:\Code\VTK\VtkGitBuild          # CMake 构建目录
D:\Code\VTK\VtkGitBuild\install  # 安装目录，供生成器和 VtkSharp.Native 使用
```

安装步骤不能省略。VtkSharp 生成器需要安装目录中的 VTK headers 和 hierarchy 文件，`VtkSharp.Native` 则通过安装后的 CMake package 查找 VTK。

## 模块范围

开发环境构建 VTK 的常规常用模块，不把模块范围严格限制为 VtkSharp 当前白名单。这样可以在持续补充 API 时减少重新编译 VTK 的次数。

当前启用以下模块组：

- `StandAlone`
- `Rendering`
- `Imaging`
- `Views`

明确排除 Qt、MPI、Web、JavaScript/WebAssembly、CUDA、HIP、Kokkos、WebGPU、Tk 和 remote modules。这些能力需要额外工具链或第三方运行环境，不属于当前 Windows 桌面 CAD/CAE 可视化开发范围。

VTK 模块开关中，`WANT` 表示依赖满足时构建，`NO` 表示禁止构建。明确排除的模块组使用 `NO`；如果后续某个必需模块依赖被设为 `NO` 的模块，CMake 会在配置阶段报错，此时应先确认是否确实需要扩大项目范围。

## 配置

在 PowerShell 中执行：

```powershell
cmake `
    -S D:/Code/VTK/VtkGitSource `
    -B D:/Code/VTK/VtkGitBuild `
    -G "Visual Studio 18 2026" `
    -A x64 `
    -DCMAKE_INSTALL_PREFIX=D:/Code/VTK/VtkGitBuild/install `
    -DBUILD_SHARED_LIBS=OFF `
    -DVTK_BUILD_ALL_MODULES=OFF `
    -DVTK_GROUP_ENABLE_StandAlone=WANT `
    -DVTK_GROUP_ENABLE_Rendering=WANT `
    -DVTK_GROUP_ENABLE_Imaging=WANT `
    -DVTK_GROUP_ENABLE_Views=WANT `
    -DVTK_GROUP_ENABLE_Qt=NO `
    -DVTK_GROUP_ENABLE_MPI=NO `
    -DVTK_GROUP_ENABLE_Web=NO `
    -DVTK_GROUP_ENABLE_Tk=NO `
    -DVTK_USE_MPI=OFF `
    -DVTK_USE_CUDA=OFF `
    -DVTK_USE_HIP=OFF `
    -DVTK_USE_KOKKOS=OFF `
    -DVTK_ENABLE_WEBGPU=OFF `
    -DVTK_WRAP_JAVASCRIPT=OFF `
    -DVTK_WRAP_PYTHON=OFF `
    -DVTK_WRAP_JAVA=OFF `
    -DVTK_WRAP_SERIALIZATION=OFF `
    -DVTK_ENABLE_REMOTE_MODULES=OFF `
    -DVTK_BUILD_TESTING=OFF `
    -DVTK_BUILD_EXAMPLES=OFF `
    -DVTK_BUILD_DOCUMENTATION=OFF `
    -DVTK_ENABLE_WRAPPING=ON `
    -DVTK_ENABLE_KITS=OFF `
    -DVTK_SMP_IMPLEMENTATION_TYPE=STDThread
```

`VTK_ENABLE_WRAPPING=ON` 用于生成 hierarchy 文件，不表示需要启用 Python、Java 或 JavaScript wrapper。

## 构建与安装

优先完成 Release 构建：

```powershell
cmake --build D:/Code/VTK/VtkGitBuild --config Release --parallel
cmake --install D:/Code/VTK/VtkGitBuild --config Release
```

需要调试 native 生命周期、渲染或互操作问题时，再构建 Debug：

```powershell
cmake --build D:/Code/VTK/VtkGitBuild --config Debug --parallel
cmake --install D:/Code/VTK/VtkGitBuild --config Debug
```

Debug 和 Release 可以安装到同一个目录。Windows 下 VTK 的 Debug 库默认带 `d` 后缀，对应的 CMake target 配置会分别引用 Debug 和 Release 库。

安装完成后，使用对应配置构建 VtkSharp native 层：

```powershell
.\tools\build-native.ps1 `
    -Configuration Release `
    -VtkDir "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.7"
```

## 后续增加 VTK 模块

如果补充 VtkSharp API 时发现安装的 VTK 缺少模块，不需要清空构建目录或完整重编全部模块。以新增 `VTK::IOXML` 为例，在原构建目录中重新配置：

```powershell
cmake `
    -S D:/Code/VTK/VtkGitSource `
    -B D:/Code/VTK/VtkGitBuild `
    -DVTK_MODULE_ENABLE_VTK_IOXML=YES

cmake --build D:/Code/VTK/VtkGitBuild --config Release --parallel
cmake --install D:/Code/VTK/VtkGitBuild --config Release
```

CMake 会增量构建 `IOXML` 及尚未构建的必要依赖。随后执行以下流程：

1. 在白名单中补充新模块的类和 API。
2. 重新运行生成器，确认 `src/bindings/VtkSharp.Native/vtksharp.modules.generated.cmake` 包含新模块。
3. 重新配置并构建 `VtkSharp.Native`。
4. 运行相关测试和最小渲染示例。

新增模块通常可以直接增量构建。如果需要关闭已构建模块、切换编译器、平台、静态/动态库或 CRT 配置，应使用新的构建目录，避免旧 CMake cache 和二进制残留造成混用。
