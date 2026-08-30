---
name: port-vtk-example
description: Translate or port a VTK C++ example from a URL, local source file, or example name into the VtkSharp C# ExampleBrowser, supplementing only the required VTK bindings through the candidate whitelist workflow. Use for VTK example translation, example migration, missing wrapper discovery, and example-specific binding additions. Do not use for unrelated VtkSharp API additions without an example.
---

# 翻译 VTK 示例

在仓库根目录工作。修改前阅读 `references/workflow.md`，了解源码获取、翻译和验证约定。

- 读取实际 C++ 源码，沿用当前 ExampleBrowser 结构，只添加示例需要的 API。
- 使用 `plan-bindings` 批量定位声明类、比对现有绑定并生成候选；仅需类型时使用 `classOnly`，不要为了添加类型而引入无关方法。
- 审核规划报告和 `diff-whitelist`，再通过 `merge-candidate` 修改正式白名单。单类旧入口 `create-candidate` 必须使用 `--supported-only`。
- `ambiguous` 用签名 ID 明确选择；`needs-metadata` 的方向、长度和所有权需检查原生契约，不能猜测。
- 回调使用 managed `AddObserver`，不直接暴露 `vtkCallbackCommand`。生成代码的问题修复在 generator/配置中，不手改生成文件。
- 完成前验证 native/managed 构建、生成一致性和目标示例。

Windows 使用 PowerShell。候选、需求与报告放在仓库外或忽略的输出目录。
