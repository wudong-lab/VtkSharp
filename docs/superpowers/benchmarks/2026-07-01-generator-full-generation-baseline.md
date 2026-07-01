# Generator Full Generation Baseline

记录日期：2026-07-01

## 场景

记录当前 generator 在未实现增量生成缓存前，执行一次完整 `generate-bindings` 的耗时，作为后续优化 benchmark 对比基线。

## 命令

```powershell
$outputRoot = Join-Path $env:TEMP ("VtkSharp.Generator\benchmark\full-" + [guid]::NewGuid().ToString("N"))
$elapsed = Measure-Command {
    dotnet run --project generator\VtkSharp.Generator.Cli -- generate-bindings --output-root $outputRoot
}
```

## 结果

- 输出目录：`%TEMP%\VtkSharp.Generator\benchmark\full-f48b679f57d64a2ea25f474aeef756ac`
- 生成结果：成功
- 总耗时：`00:02:36.0573816`
- 总秒数：`156.057`

## 备注

- 该耗时包含 `dotnet run` 启动、项目构建检查、白名单校验、VTK header inspection、C# / C++ / CMake 文件生成和写入。
- 后续如果要更精确地区分 generator 本身耗时，可先构建 CLI，再直接运行生成器 DLL 或 EXE 进行对比。
