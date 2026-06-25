# VtkSharp

非官方 VTK .NET 绑定库。C ABI shim + C# P/Invoke 路线，BSD-3-Clause。

## 目录

```
generator/                        # 绑定生成器（你操作的主要入口）
  VtkSharp.Generator.Cli/         # CLI 入口，供人工和 Codex 调用
  VtkSharp.Generator.Core/        # 解析 / 校验 / 类型映射 / 代码生成
  whitelist/                      # 正式白名单（按 VTK module 拆分 YAML）
  config/                         # 公共配置 + 本机路径
  schemas/                        # JSON Schema（白名单 / 候选 / 配置）
src/bindings/VtkSharp/            # C# 绑定输出目录（含手写 partial）
src/native/src/                   # C++ export 输出目录
src/bindings/TestConsole/         # smoke test 项目
docs/superpowers/specs/           # 设计文档
examples/                         # 示例翻译案例
```

## CLI 命令速查

从仓库根目录运行。`cd D:\Code\wudong-lab\VtkSharp`。

### 查询
```
dotnet run --project generator/VtkSharp.Generator.Cli -- inspect-class vtkActor [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- suggest-api vtkRenderer SetBackground [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- list-modules
dotnet run --project generator/VtkSharp.Generator.Cli -- list-classes [--module vtkFiltersSources] [--format json]
```

### 白名单
```
dotnet run --project generator/VtkSharp.Generator.Cli -- create-candidate vtkXxx examples/<Example>/candidate.yml --supported-only --source-kind vtk-example --source-name <Name> --source-original "path/to/original.cxx"
dotnet run --project generator/VtkSharp.Generator.Cli -- diff-whitelist examples/<Example>/candidate.yml [--format json]
dotnet run --project generator/VtkSharp.Generator.Cli -- merge-candidate examples/<Example>/candidate.yml
dotnet run --project generator/VtkSharp.Generator.Cli -- validate-whitelist [--continue-on-error]
dotnet run --project generator/VtkSharp.Generator.Cli -- normalize-whitelist
```

### 生成
```
dotnet run --project generator/VtkSharp.Generator.Cli -- generate --check
dotnet run --project generator/VtkSharp.Generator.Cli -- generate --output-root <temp-dir> [--continue-on-error]
```

### 构建
```
cmake --build src/native/out/build/windows-x64-vs2022 --config Release
dotnet build src/bindings/VtkSharp.slnx
cp src/native/out/build/windows-x64-vs2022/Release/vtksharp_native.dll src/bindings/TestConsole/bin/Debug/net48/
PATH="C:/Program Files/VTK/bin:$PATH" dotnet run --project src/bindings/TestConsole -- --smoke
```

## Codex 示例翻译流程（8 步）

设计文档: `docs/superpowers/specs/2026-06-23-vtksharp-generator-design.md` 行 801-811。

1. 选择 VTK C++ 示例，翻译到 C#，放入 `examples/<Name>/`
2. 构建并运行示例：`dotnet build src/bindings/VtkSharp.slnx`
3. 解析缺失类/成员（编译错误）
4. 对每个缺失类调 CLI 查询：`create-candidate vtkXxx ... --supported-only`
5. 生成候选白名单到 `examples/<Name>/candidate.yml`
6. 人工审核 candidate.yml
7. `merge-candidate examples/<Name>/candidate.yml`（自动 normalize）
8. `generate --check` → cmake build → dotnet build → smoke test

## 约束

- 候选白名单不直接修改正式白名单，必须通过 merge-candidate 且人工审核
- 白名单是强契约：类型不支持 / 签名不匹配 / 类不存在 → 报错停止（可用 --continue-on-error 探索）
- `--supported-only` 过滤不支持的类型：`unsigned long`、`int&`、`basic_ostream&`、非指针值类型 class 等都会被排除
- `vtkObjectBase` / `vtkObject` 是 manualBindingClasses，生成器跳过
- 手写 partial 类不进入白名单
- C# 项目 multi-target: `netstandard2.0;net10.0`，生成代码使用 `#if NET10_0_OR_GREATER` 条件编译
- `src/bindings/VtkSharp/VtkString.cs` 是手写 runtime helper，不由生成器输出
- 正式白名单必须通过 `git diff` 审核后才能提交
