# 贡献指南

欢迎提交问题、示例、文档修正和最小范围的绑定改进。无需使用 AI 工具即可参与开发。

## 报告问题与提出需求

先搜索 [现有 Issues](https://github.com/wudong-lab/VtkSharp/issues)，避免重复。
Bug 报告请提供最小复现、期望与实际行为、VTK/.NET/VS/CMake 版本、x64 架构、Debug/Release 配置和相关日志。
渲染问题另附 GPU、驱动及截图；不要上传项目机密、个人路径中的敏感数据或访问凭据。
新增 API 需求应说明 VTK 类、方法和实际使用场景，最好附一个上游 C++ 示例。

## 开发环境

按 [README](README.md) 配置工具链、VTK 安装以及 `VTK_ROOT` / `VTK_DIR`。
完整开发需要 .NET 10 SDK（生成器）和 .NET 8 SDK/运行时（绑定测试与 WPF 示例）。
个人路径使用环境变量或被忽略的 `vtksharp.generator.local.yml`，不要写入共享配置。

## 修改边界

- 沿用当前项目结构，保持改动聚焦；public API、对象生命周期、线程模型或 ABI 变更应先在 Issue/PR 中讨论。
- 不直接修改生成的 wrapper 和 C ABI 导出；通过候选白名单和生成器更新。手写 partial 与 helper 保持手工维护。
- 新 API 使用 `create-candidate` / `plan-bindings`、`diff-whitelist`、`merge-candidate` 流程，详见 [生成器文档](docs/generator.md)。
- 指针、数组、回调和对象返回值应明确方向、长度、所有权和有效期，不能仅以“编译通过”作为安全依据。
- 新能力优先附一个目标单一的测试或示例；移植代码、数据和素材应记录上游来源及许可，不提交未经授权的内容。

## 验证与 Pull Request

从仓库根目录按改动范围运行检查，并在 PR 描述中写明执行结果及未验证项：

```powershell
# Script environment-variable contract (no VTK installation required)
pwsh -NoProfile -File tools/test-build-environment.ps1

# Generator-only tests
dotnet test src/generator/VtkSharp.Generator.Tests --configuration Release

# Full binding workflow; add -Regenerate after changing the whitelist
.\tools\verify-workflow.ps1 -Example GeometricObjects/Cone
```

完整流程包括生成器测试、native 构建、绑定测试、示例构建和生成一致性检查。
仅 Cone 目前提供自动截图验收；其他示例应按其支持情况选择或手动运行，不能用 Cone 代替目标示例验收。
图像正确性、交互、重复创建/销毁和回调保活仍需人工确认，详见 [验证说明](docs/workflow/verification.md)。

PR 请说明改了什么、为什么改、是否影响兼容性，以及复现或验证命令。
不要提交构建产物、机器本地配置或无关格式化。AI 辅助开发可参考 [协作规范](docs/workflow/ai-assisted-development.md)，但不是贡献前提。
