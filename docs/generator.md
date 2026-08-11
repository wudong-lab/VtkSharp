# VtkSharp 绑定生成器

## 职责与输入

生成器位于 `src/generator`，其主要输入为：

- `config/vtksharp.generator.yml`：VTK 版本、输出目录和生成行为。
- `whitelist/`：按 VTK module 划分的正式 API 白名单。
- `schemas/`：配置、白名单和候选文件的 JSON Schema。
- VTK 安装目录中的 headers 与 hierarchy 文件。

输出写入：

```text
src/bindings/VtkSharp/            # C# wrapper
src/bindings/VtkSharp.Native/src/ # C++ export
src/bindings/VtkSharp.Native/     # CMake 与 module 集合
```

## 导出规则

- 只生成当前 VTK 类直接声明的 public 实例成员函数。
- 继承但未重新声明的函数由 C# wrapper 继承，不在派生类重复导出。
- 当前类声明同名函数时遵循 C++ 名称隐藏规则，不把被隐藏的基类 overload 当作候选。
- C# 调用语义与通过对应 C++ 静态类型指针或引用调用一致；virtual 函数仍由 C++ 动态分派。
- 静态函数只特殊支持 `static New()`；其他 static 函数以及构造、析构函数默认忽略。
- 候选列表、白名单校验和最终生成必须使用同一套可导出规则。
- 生成器不覆盖手写 partial、runtime helper 和官方类型的手工补充导出。

## 常用命令

从仓库根目录运行：

```powershell
# 查询
dotnet run --project src/generator/VtkSharp.Generator.Cli -- inspect-class vtkActor
dotnet run --project src/generator/VtkSharp.Generator.Cli -- inspect-function vtkRenderer SetBackground
dotnet run --project src/generator/VtkSharp.Generator.Cli -- list-modules

# 白名单
dotnet run --project src/generator/VtkSharp.Generator.Cli -- create-candidate vtkXxx -o candidate.yml --supported-only
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist candidate.yml
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate candidate.yml
dotnet run --project src/generator/VtkSharp.Generator.Cli -- validate-whitelist

# 生成与一致性检查
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src --incremental
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

日常迭代使用 `--incremental`。提交前使用 `--check` 全量生成到临时目录并与当前输出比较。

## 白名单变更流程

1. 通过示例或 API 需求确定最小类和成员集合。
2. 使用 `create-candidate` 产生候选文件。
3. 人工检查签名、所有权和类型映射。
4. 使用 `diff-whitelist` 查看正式白名单变化。
5. 使用 `merge-candidate` 合并并规范化。
6. 运行白名单校验、全量生成检查、native 构建和 managed 测试。

YamlDotNet 直接反序列化的 DTO 集合应使用 `List<T>`、`Dictionary<TKey, TValue>` 等具体可变类型，避免接口集合无法构造或填充。
