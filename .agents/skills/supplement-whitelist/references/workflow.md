# 参考导出接口补充工作流

## 扫描与批量规划

```powershell
.\.agents\skills\supplement-whitelist\scripts\scan-reference-exports.ps1 `
    -ReferenceDirectory <path> -OutputPath <temporary-scan.json>

dotnet run --project src/generator/VtkSharp.Generator.Cli -- plan-bindings `
    --reference-scan --requests <temporary-scan.json> `
    --output <temporary-candidate.yml> --report <temporary-report.json>
```

扫描脚本负责递归查找、类名提取、移除 overload 数字后缀、跳过 New 和去重。检查扫描警告；规划器拒绝含警告的扫描结果。

参考模式明确选择扫描方法名的全部可直接生成重载，空方法集合只请求类型。CLI 查询当前安装的 VTK，不信任参考文件签名；只将未导出的受支持函数加入候选，并保留需要的新接收类型。

先读终端摘要，只为待处理项打开详细 JSON。使用 `tools/read-binding-report.ps1 -Path <report.json> -Class <VTKClass>` 或 `-Status needs-metadata` 筛选，不需要重复加载完整成功报告：

- `already-exported`：无需添加。
- `needs-metadata`：需查明指针方向、长度等契约，不按名称猜测。
- `unsupported` / `not-found`：归因到不支持的类型、手写 API、声明隐藏或 VTK 版本差异，不能静默忽略。
- `ambiguous`：检查声明类或使用精确签名请求。复杂 C++ 查找不由工具猜测。

存在未解决项时规划退出码为 1，但仍写入可处理部分的 candidate。明确记录哪些接口被排除，再决定是否合并该部分。需求 JSON 和详细状态规则见 `docs/generator.md` 的“批量接口规划”。

## 审核、合并与验证

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist <temporary-candidate.yml> --summary
# 审核后执行
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate <temporary-candidate.yml>
```

完整 diff 包含基类、签名依赖和新增模块，merge 先校验再写入，并自动规范化。不再重复执行 normalize 或人工逐类补基类。所有权等元数据冲突不能通过新增接口流程覆盖。

从当前 VTK 安装确认 `VTKConfig.cmake` 所在目录，不照抄旧版本路径：

```powershell
$vtkDir = "<vtk-cmake-directory>"
.\tools\verify-workflow.ps1 -VtkDir $vtkDir -Regenerate
```

生成器负责模块列表；模块缺失应修正 generator/config 输入，不手改生成 CMake。报告引用扫描与规划统计、新增依赖、排除项及实际验证结果，不重新逐条抄写工具清单。

统一验证脚本的阶段状态、日志和可选示例验收见 `docs/workflow/verification.md`。没有指定目标示例时截图验收为未执行，不应宣称渲染通过。方向、长度、所有权需要人工确认时，按 `docs/workflow/interop-evidence.md` 保存源码依据与复核条件，不只保存在临时报告中。
