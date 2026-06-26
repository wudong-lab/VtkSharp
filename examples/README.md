# VtkSharp Examples

所有示例代码统一由 `ExampleBrowser` 管理，按 VTK 分类放在子目录中。

## 目录约定

```
examples/
  ExampleBrowser/
    Examples/
      <Category>/
        <ExampleName>/
          <ExampleName>.cs        # C# 翻译代码（实现 IExample 接口）
          candidate.yml           # 候选白名单（create-candidate 输出）
          porting-notes.md        # 翻译记录：遇到的缺失 API、决策、未解决的问题
```

## 运行示例浏览器

```bash
dotnet run --project examples/ExampleBrowser/ExampleBrowser.csproj
```

## 添加新示例

1. 找到 VTK C++ 示例源码（`VTK_ROOT/Examples/...`），确定分类（如 `GeometricObjects`、`Modelling` 等）
2. 在 `examples/ExampleBrowser/Examples/<Category>/<ExampleName>/` 下创建 C# 翻译代码
3. 实现 `IExample` 接口，标注 `[Example]` Attribute
4. 构建 → 编译错误 → `create-candidate` + `inspect-function` 查缺失 API
5. 把缺失 API 写成 `candidate.yml`
6. `diff-whitelist` 审查 → `merge-candidate` 合并
7. `validate-whitelist` → `generate-bindings --check` → cmake build → dotnet build

## 示例选择优先级

- 优先选 **VTK SDK 自带示例**（`Cone`, `Cylinder`, `Sphere` 等）
- 优先选 **已有的 TestConsole 覆盖不到的 API**
- 第一次走通流程后再挑战复杂示例
