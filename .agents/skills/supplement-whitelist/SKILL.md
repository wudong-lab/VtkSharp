---
name: supplement-whitelist
description: Import or supplement VtkSharp bindings by scanning a reference directory of VTK-style *_export_gen.cpp files, extracting referenced classes and methods, and adding supported APIs through the candidate whitelist workflow. Use when comparing or importing interfaces from VtkNet or another generated native export set. Do not use for a single known API or VTK example translation.
---

# 从参考导出补充白名单

在仓库根目录工作。修改绑定前阅读 `references/workflow.md`。

- 用 `scripts/scan-reference-exports.ps1` 执行确定性提取，不临时重写解析器。扫描名称只作为请求，不代表当前 VTK 的签名。
- 将扫描 JSON 直接交给 `plan-bindings --reference-scan`，由 CLI 完成声明类定位、分类、去重和 candidate 生成。
- 审核扫描警告、规划报告和完整 diff，再通过 `merge-candidate` 修改正式白名单。报告有未解决项时不能宣称完整导入。
- merge 自动补齐基类和签名依赖；不再逐类人工补齐。依赖无法解析时分析报告，不绕过校验。
- 不手改生成的 wrapper、export 或 CMake 模块列表；验证生成一致性、native 构建和 managed 测试。

Windows 使用 PowerShell。需求、候选和报告放在仓库外或忽略的输出目录。
