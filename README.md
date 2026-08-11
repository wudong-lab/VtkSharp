# VtkSharp

非官方 VTK .NET 绑定库，采用 C ABI shim + C# P/Invoke 路线，面向 CAD/CAE 可视化场景。

[![License](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](LICENSE)

## 特性

- 基于白名单的绑定生成器，按需导出 VTK API
- 支持 `netstandard2.0` 和 `net8.0` 多目标框架
- 示例浏览器，包含几何对象、建模等分类示例

## 结构

```
src/generator/        # 绑定生成器（CLI / 核心 / 白名单 / 配置）
src/bindings/         # VTK 官方 API 的 C# 绑定输出 + C++ export
src/examples/         # 示例浏览器 + VTK 官方示例翻译
```

当前架构、生成规则和开发流程见 [项目文档](docs/README.md)。

## 快速开始

### 构建

首次构建前，先在仓库根目录运行 VTK 构建脚本。脚本根据 `VtkSharp` 与 VTK 工作目录的固定相对位置查找 `VtkGitSource`，默认完成 VTK 9.6.2 的 Release 配置、构建和安装：

VTK 工作目录结构如下：

```text
D:\Code\VTK\
├── VtkGitSource\         # VTK 9.6.2 源码
└── VtkGitBuild\          # CMake 构建目录
    └── install\          # 安装目录，供生成器和 VtkSharp.Native 使用
```

```powershell
.\tools\build-vtk-for-vtksharp.ps1
```

需要同时构建并安装 Debug 和 Release 时：

```powershell
.\tools\build-vtk-for-vtksharp.ps1 -Configuration Both
```

只生成 Visual Studio 2026 构建工程、不执行编译和安装时：

```powershell
.\tools\build-vtk-for-vtksharp.ps1 -Action Configure
```

详细的模块和目录约定见 [VTK 构建说明](docs/build/vtk.md)。VTK 安装完成后，先设置
`VtkDir`。它必须指向包含 `VTKConfig.cmake`（或 `vtk-config.cmake`）的 CMake package
目录；VTK 构建脚本完成安装时也会输出这个目录：

```powershell
$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.6"

# Release
.\tools\build-all.ps1 -Configuration Release -VtkDir $vtkDir

# Debug（需要先使用 -Configuration Both 构建并安装 Debug VTK）
.\tools\build-native.ps1 -Configuration Debug -VtkDir $vtkDir
dotnet build src/bindings/VtkSharp.slnx --configuration Debug
```

构建脚本不写死 VTK 安装目录。未传入 `-VtkDir` 时，CMake 只能通过已有缓存或自身的
package 搜索规则查找 VTK，因此推荐始终显式传入。

### 示例浏览器

```powershell
dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj
dotnet run --project src/examples/ExampleBrowser/ExampleBrowser.csproj
```

### 生成绑定

```powershell
# 增量生成（日常开发）
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src --incremental

# 全量检查（提交前）
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

## 许可

[BSD-3-Clause](LICENSE)
