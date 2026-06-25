# VtkSharp Examples

每个子目录对应一个 VTK C++ 示例的翻译结果。

## 目录约定

```
examples/
  <ExampleName>/
    <ExampleName>.cs        # C# 翻译代码（可运行）
    candidate.yml           # 候选白名单（create-candidate 输出）
    porting-notes.md        # 翻译记录：遇到的缺失 API、决策、未解决的问题
```

## 工作流

1. 找到 VTK C++ 示例源码（`VTK_ROOT/Examples/...`）
2. 写 C# 翻译版 `examples/<Name>/<Name>.cs`
3. 构建 → 编译错误 → `create-candidate` + `inspect-function` 查缺失 API
4. 把缺失 API 写成 `candidate.yml`
5. `diff-whitelist` 审查 → `merge-candidate` 合并
6. `validate-whitelist` → `generate-bindings --check` → build → smoke

## 示例选择优先级

- 优先选 **VTK SDK 自带示例**（`Cone`, `Cylinder`, `Sphere` 等）
- 优先选 **已有的 TestConsole 覆盖不到的 API**
- 第一次走通流程后再挑战复杂示例
