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
dotnet run --project src/generator/VtkSharp.Generator.Cli -- list-classes --module vtkFiltersSources

# 白名单
dotnet run --project src/generator/VtkSharp.Generator.Cli -- create-candidate vtkXxx -o candidate.yml --supported-only --source-kind manual --methods Method1 Method2
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist candidate.yml
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate candidate.yml
dotnet run --project src/generator/VtkSharp.Generator.Cli -- validate-whitelist
dotnet run --project src/generator/VtkSharp.Generator.Cli -- normalize-whitelist

# 生成与一致性检查
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src --incremental
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

日常迭代使用 `--incremental`。提交前使用 `--check` 全量生成到临时目录并与当前输出比较。

查询类命令支持 `--format json`，适合脚本和 AI 读取结构化结果。`create-candidate` 的常用参数：

- `--methods`：只包含指定方法；不传时包含该类所有可导出方法。
- `--supported-only`：排除当前类型映射不支持的签名。
- `--skip-missing-methods`：批处理时跳过当前 VTK 版本不存在的方法并输出警告。
- `--source-kind`、`--source-name`、`--source-original`：记录候选来源。

`validate-whitelist` 和生成命令支持 `--continue-on-error` 进行探索，但正式提交前必须在默认快速失败模式下通过。

## 增量生成

`--incremental` 使用模块目录中的 `.vtksharp.generated.json` 清单按类复用已有输出。以下内容变化时会重新生成相应类型：

- whitelist 条目；
- VTK header；
- generator 配置或 cache version；
- 已生成文件内容。

怀疑缓存失效时使用 `--incremental --force`。`--check` 始终在临时目录执行全量生成并与仓库输出比较，不依赖增量缓存。

## 白名单变更流程

1. 通过示例或 API 需求确定最小类和成员集合。
2. 使用 `create-candidate` 产生候选文件。
3. 人工检查签名、所有权和类型映射。
4. 使用 `diff-whitelist` 查看正式白名单变化。
5. 使用 `merge-candidate` 合并并规范化。
6. 运行白名单校验、全量生成检查、native 构建和 managed 测试。

正式 whitelist 是强契约。新增接口时还应遵守：

- 不直接编辑正式 whitelist；通过 candidate、diff 和 merge 流程修改。
- `New()` 由生成器识别，不作为普通 whitelist 方法添加。
- 新类型的基类链必须已有 wrapper，否则生成的 C# 代码无法编译。
- 手写 partial、runtime helper 和手工 C ABI 导出不进入正式 whitelist。
- 指针返回、引用参数、回调或其他复杂所有权边界必须单独审核；不能因为生成器能够解析就默认 public API 安全。

YamlDotNet 直接反序列化的 DTO 集合应使用 `List<T>`、`Dictionary<TKey, TValue>` 等具体可变类型，避免接口集合无法构造或填充。
