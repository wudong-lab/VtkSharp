# VtkSharp

非官方 VTK .NET 绑定库，采用 C ABI shim + C# P/Invoke 路线，面向 CAD/CAE 可视化场景。

[![License](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](LICENSE)

## 特性

- 基于白名单的绑定生成器，按需导出 VTK API
- 支持 `netstandard2.0` 和 `net8.0` 多目标框架
- WPF 渲染控件（`VtkSharp.Wpf`），提供 `VTKRenderControl`
- 示例浏览器，包含几何对象、建模等分类示例

## 结构

```
src/generator/        # 绑定生成器（CLI / 核心 / 白名单 / 配置）
src/bindings/         # C# 绑定输出 + C++ export（VtkSharp / VtkSharp.Native / VtkSharp.Wpf）
src/examples/         # WPF 示例浏览器 + 翻译案例
```

## 快速开始

### 构建

```powershell
# Debug
.\tools\build-native.ps1 -Configuration Debug
dotnet build src/bindings/VtkSharp.slnx --configuration Debug

# Release
.\tools\build-all.ps1 -Configuration Release
```

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
