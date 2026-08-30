# 统一验证与示例验收

`tools/verify-workflow.ps1` 复用现有生成器、构建脚本和测试，不修改白名单，也不自动提交。需要 PowerShell 7、项目所需的 .NET SDK、C++ 工具链和已安装的 VTK。

## 常用入口

在仓库根目录运行；先按 [README](../../README.md#2-设置-vtk-环境变量) 设置 `VTK_ROOT` 和
`VTK_DIR`。脚本默认读取环境变量 `VTK_DIR`，也可用 `-VtkDir` 覆盖，目录必须包含
`VTKConfig.cmake` 或 `vtk-config.cmake`。未提供路径时在创建报告目录前报错。

```powershell
.\tools\verify-workflow.ps1 -Example GeometricObjects/Cone

# 合并 candidate 后，需要更新生成文件时显式启用
.\tools\verify-workflow.ps1 -Regenerate -Example GeometricObjects/Cone
```

默认使用 Release，依次构建 CLI、运行 generator 测试、按需增量生成、构建 native、运行 managed 测试、构建 ExampleBrowser、按需运行目标示例、检查生成一致性。没有指定 `-Example` 时，示例验收标记为 `not-run`，不会自动用 Cone 代替目标示例。

可通过 `-Configuration`、`-GeneratorConfig`、`-VtkBinDirectory` 指定配置；默认 VTK DLL 目录是 CMake 包目录的 `../../../bin`，只加入子进程 PATH。生成器使用 `VTK_ROOT` 或本地配置中的安装，脚本不会根据 `-VtkDir` 自动推导并覆盖它；调用者应确保两者是同一版本和安装。

每次运行创建新的 `artifacts/verification/<timestamp-id>/`，也可用 `-OutputDirectory` 指定不存在的目录。保留完整 stdout/stderr、每阶段命令、退出码、耗时、警告摘要及 `verification.json`。报告记录提交和工作区状态，但不是源码快照；源码或配置变化后应重新验证。

任何阶段失败或超时都会返回非零退出码，并把后续选中阶段标记为 `not-run / earlier-stage-failed`。脚本不会自动重试或修复。默认单阶段上限 1800 秒，示例上限 60 秒，可分别用 `-StageTimeoutSeconds`、`-ExampleTimeoutSeconds` 调整。超时会终止该阶段的进程树。

终端只显示阶段结果、有限的警告/失败摘要和日志路径。警告没有与历史基线比较，不应称为“新增警告”；需要完整诊断时再读取对应日志。`passed` 仅表示本次选中的自动检查通过，不能替代未执行项和人工验收。

## 示例自动验收

不带参数启动 ExampleBrowser 仍打开原来的浏览器界面。自动验收按 `Category/Name` 精确选择示例：

```powershell
& .\src\examples\ExampleBrowser\bin\Release\net8.0-windows\ExampleBrowser.exe `
    --smoke GeometricObjects/Cone --output <new-output-directory>
```

该入口在 STA 线程运行，不打开浏览器界面；仍可能创建 VTK 渲染窗口，需要可用的图形环境，不承诺无显示设备运行。直接从 PowerShell 启动 WinExe 时 shell 可能不等待退出，自动化应使用统一验证脚本。

示例通过可选 `ISmokeExample.RenderScreenshot` 提供能力，首个实现是 Cone。未实现该接口、名称错误、参数错误或输出目录已存在都返回非零退出码；不会回退调用阻塞式 `Run()`。

适配示例时让交互和截图模式共用场景构造，仅截图模式跳过 `interactor.Start()`。在渲染后、对象释放前写出 PNG，方法返回前释放本次创建的资源。运行器确认 PNG 可解码后写出 `result.json`，包含图像尺寸和仍需人工检查的项目。

自动通过只说明渲染调用返回、截图可读取、进程正常退出。还需查看截图的几何和相机效果，手动验证交互，以及按任务需要验证重复创建/销毁、回调保活和宿主线程约束。当前没有自动图像基线比较，也没有泄漏检测；不要以 PNG 字节一致作为跨 GPU 的通用标准。

## 规划报告按需阅读

规划时先读 CLI 摘要；需要重新查看报告时：

```powershell
.\tools\read-binding-report.ps1 -Path <plan-report.json>
.\tools\read-binding-report.ps1 -Path <plan-report.json> -Class vtkRenderer
.\tools\read-binding-report.ps1 -Path <plan-report.json> -Status needs-metadata
```

默认摘要包含全部新增类、依赖原因、新增函数、冲突和未解决项，不展开成功诊断的详细签名。筛选模式返回 JSON，按请求类或声明类精确匹配；这是只读工具，退出码 0 仅代表读取成功，不代表规划通过。最终合并前仍执行 `diff-whitelist --summary`，因为磁盘报告可能早于当前白名单。

无需把工具清单再次逐条抄入说明文档。交接时记录报告路径、实际执行结果、移植差异和排除项；需要长期保留的判断依据按 [互操作依据记录](interop-evidence.md) 写入版本控制。
