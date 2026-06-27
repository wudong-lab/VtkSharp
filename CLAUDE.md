# VtkSharp

非官方 VTK .NET 绑定库。C ABI shim + C# P/Invoke 路线，BSD-3-Clause。

## 目录

```
generator/                        # 绑定生成器（你操作的主要入口）
  VtkSharp.Generator.Cli/         # CLI 入口，供人工和 AI 调用
  VtkSharp.Generator.Core/        # 解析 / 校验 / 类型映射 / 代码生成
  whitelist/                      # 正式白名单（按 VTK module 拆分 YAML）
  config/                         # 公共配置 + 本机路径
  schemas/                        # JSON Schema（白名单 / 候选 / 配置）
src/bindings/VtkSharp/            # C# 绑定输出目录（含手写 partial）
src/native/src/                   # C++ export 输出目录


docs/superpowers/specs/           # 设计文档
examples/
  ExampleBrowser/                 # 示例浏览器（Avalonia 桌面应用）
    Examples/                     # 示例翻译案例（按 VTK 分类）
      GeometricObjects/           # 几何对象类示例
      Modelling/                  # 建模类示例
```

## CLI 命令速查

从仓库根目录运行。

### 查询
```
dotnet run --project generator/VtkSharp.Generator.Cli -- inspect-class vtkActor [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- inspect-function vtkRenderer SetBackground [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- list-modules
dotnet run --project generator/VtkSharp.Generator.Cli -- list-classes [--module vtkFiltersSources] [--format json]
```

### 白名单
```
dotnet run --project generator/VtkSharp.Generator.Cli -- create-candidate vtkXxx -o examples/ExampleBrowser/Examples/<Category>/<Name>/candidate.yml --supported-only --source-kind vtk-example --source-name <Name> --source-original "path/to/original.cxx" [--methods Method1 Method2 ...]
dotnet run --project generator/VtkSharp.Generator.Cli -- diff-whitelist examples/ExampleBrowser/Examples/<Category>/<Name>/candidate.yml [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- merge-candidate examples/ExampleBrowser/Examples/<Category>/<Name>/candidate.yml
dotnet run --project generator/VtkSharp.Generator.Cli -- validate-whitelist [--continue-on-error] [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- normalize-whitelist
```

`--methods` 可指定只输出需要的方法名（空格分隔），不传则输出类的全部方法；若指定了不存在的方法名会报错退出。

### 生成
```
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --check
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --output-root <dir> [--continue-on-error]
```

### 构建

> **⚠ CRT 匹配要求**：native DLL 与 VTK DLL 必须使用相同的 CRT（`/MD` 对 Release、`/MDd` 对 Debug）。构建配置必须一致：
> `VtkSharp.csproj` 已按 `$(Configuration)` 自动选择对应版本的 `vtksharp_native.dll`。
>
> | C# 构建 | Native 构建 | CRT |
> | --- | --- | --- |
> | `-c Debug` | `--config Debug` | `/MDd` |
> | `-c Release` | `--config Release` | `/MD` |

```
# Debug
cmake --build --preset windows-x64-debug
dotnet build src/bindings/VtkSharp.slnx --configuration Debug

# Release
cmake --build --preset windows-x64
dotnet build src/bindings/VtkSharp.slnx
```

### 示例浏览器
```
dotnet build examples/ExampleBrowser/ExampleBrowser.csproj
dotnet run --project examples/ExampleBrowser/ExampleBrowser.csproj
```

## AI 示例翻译流程（8 步）

设计文档: `docs/superpowers/specs/2026-06-23-vtksharp-generator-design.md` 行 801-811。

1. 选择 VTK C++ 示例，翻译到 C#，放入 `examples/ExampleBrowser/Examples/<Category>/<Name>/`
2. 实现 `IExample` 接口，标注 `[Example]` Attribute（含 Name、Category、Description、SourceFiles）
3. 构建：`dotnet build examples/ExampleBrowser/ExampleBrowser.csproj`
4. 解析缺失类/成员（编译错误）
5. 对每个缺失类调 CLI 查询：`create-candidate vtkXxx -o examples/ExampleBrowser/Examples/<Category>/<Name>/candidate.yml --supported-only --source-kind vtk-example --source-name <Name> --source-original <path>`
6. 人工审核 candidate.yml
7. `merge-candidate examples/ExampleBrowser/Examples/<Category>/<Name>/candidate.yml`（自动 normalize）
8. `generate-bindings --check` → cmake build → dotnet build → smoke test

## 约束

- 候选白名单不直接修改正式白名单，必须通过 merge-candidate 且人工审核
- 白名单是强契约：类型不支持 / 签名不匹配 / 类不存在 → 报错停止（可用 --continue-on-error 探索）
- `--supported-only` 过滤不支持的类型：`unsigned long`、`int&`、`basic_ostream&`、非指针值类型 class 等都会被排除
- `vtkObjectBase` / `vtkObject` 是 manualBindingClasses，生成器跳过
- 手写 partial 类不进入白名单
- C# 项目 multi-target: `netstandard2.0;net10.0`，生成代码使用 `#if NET10_0_OR_GREATER` 条件编译
- `src/bindings/VtkSharp/VtkString.cs` 是手写 runtime helper，不由生成器输出
- 正式白名单必须通过 `git diff` 审核后才能提交
