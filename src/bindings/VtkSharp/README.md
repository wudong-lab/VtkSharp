# VtkSharp

非官方 [VTK](https://vtk.org/) .NET 绑定库，使用 C ABI shim + C# P/Invoke，无需 C++/CLI。
本项目与 Kitware 无隶属关系，也未经其背书。

## 支持范围

- 托管目标框架：`netstandard2.0`、`net8.0`。
- 包内 native 资产：`runtimes/win-x64/native/VtkSharp.Native.dll`，面向 Windows x64。
- API 按白名单逐步补充，并非完整 VTK 封装；不承诺与其他 .NET 绑定源码兼容。
- `netstandard2.0` 不意味着 native 层支持 Linux/macOS；消费进程必须为 x64。
- Release native 使用动态 MSVC CRT，目标机器需匹配的 x64 Visual C++ 运行库。
- 托管库和 native DLL 应来自配套构建，不混用不同版本。

## 最小渲染示例

在已引用 VtkSharp 且能加载 native DLL 的应用中运行：

```csharp
using VtkSharp;

using var cone = vtkConeSource.New();
cone.SetResolution(32);
using var mapper = vtkPolyDataMapper.New();
mapper.SetInputConnection(cone.GetOutputPort());
using var actor = vtkActor.New();
actor.SetMapper(mapper);
using var renderer = vtkRenderer.New();
renderer.AddActor(actor);
using var window = vtkRenderWindow.New();
window.AddRenderer(renderer);
window.SetSize(800, 600);
using var interactor = vtkRenderWindowInteractor.New();
interactor.SetRenderWindow(window);
window.Render();
interactor.Start();
```

应看到可交互的圆锥窗口。`New()` 返回的拥有型 wrapper 应使用 `using` / `Dispose()`。
源码构建、示例浏览器及对象所有权说明见 [项目仓库](https://github.com/wudong-lab/VtkSharp)。
若 DLL 加载失败，检查 x64 架构、输出目录中的 native 资产和 MSVC 运行库。

## 许可

VtkSharp 使用 BSD-3-Clause；VTK 声明随包提供于 `VTK-LICENSE.txt`。
VTK 及其第三方依赖保留各自许可，分发时应保留所包含依赖的声明。
