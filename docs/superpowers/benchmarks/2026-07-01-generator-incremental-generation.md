# Generator Incremental Generation Benchmark

记录日期：2026-07-01

## 场景

记录 `generate-bindings --incremental` 在临时输出目录上的两次连续运行耗时。第一次运行没有 manifest，会建立完整缓存；第二次运行复用所有未变化类。

## 命令

```powershell
$outputRoot = Join-Path $env:TEMP ("VtkSharp.Generator\incremental-check\" + [guid]::NewGuid().ToString("N"))
$first = Measure-Command {
    dotnet run --project generator\VtkSharp.Generator.Cli -- generate-bindings --output-root $outputRoot --incremental
}
$second = Measure-Command {
    dotnet run --project generator\VtkSharp.Generator.Cli -- generate-bindings --output-root $outputRoot --incremental
}
```

## 结果

- 输出目录：`%TEMP%\VtkSharp.Generator\incremental-check\6d37974abd1846cc9f12334a25503d89`
- 第一次运行：`169.536` 秒
- 第二次运行：`2.974` 秒

## 备注

- 第一次运行需要完整 inspect、validate、emit、write，并写入各 module 的 `.vtksharp.generated.json`。
- 第二次运行命中缓存，跳过未变化类的 inspect、validate、emit 和 write。
- `--check` 仍然保持全量生成和 diff 语义，不使用增量缓存。
