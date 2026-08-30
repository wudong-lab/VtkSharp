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
dotnet run --project src/generator/VtkSharp.Generator.Cli -- inspect-function vtkRenderer SetBackground --resolve
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
- `--supported-only`：排除不支持或缺少指针方向/长度元数据的签名，并报告原因。
- `--class-only`：仅创建类型，不选择普通方法；与 `--methods` 互斥。
- `--skip-missing-methods`：批处理时跳过当前 VTK 版本不存在的方法并输出警告。
- `--source-kind`、`--source-name`、`--source-original`：记录候选来源。

`validate-whitelist` 和生成命令支持 `--continue-on-error` 进行探索，但正式提交前必须在默认快速失败模式下通过。

## XML API 注释

绑定生成器自动从当前 VTK 安装目录的原始头文件提取英文注释，为生成的类型、`New()` 和方法输出 XML 文档。重点保留功能、参数和返回值语义，并结合当前绑定规则补充所有权和确定的数组长度说明。这个过程完全由本地 C# 代码完成，不联网、不调用 AI、不消耗模型 token，也不需要 Doxygen。

- 通过 `@class` 查找类型说明，通过 CppAst 声明的 UTF-8 源码偏移关联方法说明；不使用 Clang 自动继承的注释。
- 支持 `/** ... */`、`/*! ... */`、`///`、`//!` 和 `///@{ ... ///@}` 共享组；宏展开的多个方法复用宏调用处的注释。
- 首段（或 `@brief` 所在首段）作为 `summary`，其余段落作为 `remarks`；XML 特殊字符自动转义。
- 方法按名称、规范化返回类型和参数类型匹配白名单，重载不混用注释，参数重命名不影响匹配。
- `@param` / `\param` 转换成 `param`，先匹配原生参数名，再按签名中的参数位置映射到最终 C# 参数名；共享组中不属于当前方法的参数说明不输出。支持多行说明和 `[in]` / `[out]` / `[in,out]`。仅部分参数有说明时，其余参数使用空标签，避免 CS1573，不编造说明。
- `@return` / `@returns` / `@retval`（及反斜杠形式）转换成 `returns`；`void` 方法不输出返回值说明。不从任意自然语言段落猜测参数或返回值说明，已有普通功能描述仍保留在 `summary` / `remarks`。
- `New()` 和 `ownership: owned` 的 VTK 对象返回值注明包装持有原生引用，调用者应调用 `Dispose()` 释放该引用；其他 VTK 对象返回值注明借用、不增加引用、`Dispose()` 不释放借用引用，使用期间必须保证原生对象仍存活。这描述的是现有包装行为，不代表生成器已验证默认 borrowed 符合所有原生契约；不推断具体持有者或失效时机。
- 生成的静态辅助方法也带有固定的所有权说明：`FromBorrowedPointer` 仅借用；`TakeReference` 接管一个已有引用，不增加引用计数，同一引用不可重复转交或另行释放；`Register(sourceObject)` 对同一原生对象增加一个引用，由返回包装独立负责释放，既不复制原生对象，也不改变源包装的所有权。
- 字符串和值类型返回值注明已复制到托管值，调用者无需释放返回值对应的原生内存；未知所有权的原始数组指针不补写释放规则。生成时若识别到上游明确要求 caller 负责 deleting/freeing、但包装使用 borrowed，会输出文档警告供人工核查；只匹配明确模式，不自动修改所有权配置。
- 参数数组项数来自固定数组类型（如 `double[3]`）或已有白名单 `length.kind: fixed/parameter`，以元素为单位说明，不增加运行时长度检查。显式参数/返回说明中的项数及有效期按原文保留；返回数组指针项数未知时不提供项数注释，不从方法名猜测。白名单的方向、长度和所有权元数据会做针对性校验。
- `@sa` / `@see` / `@seealso` 统一转换为普通文字 `See also: ...`，不解析符号、不生成 `cref`；简单行内格式指令转换成文字。代码块、verbatim、围栏代码及其他未支持指令可以舍弃；不翻译或补写缺失语义。
- 不求值 C++ 条件编译，跨预处理指令清除待关联注释及共享组状态，避免把另一分支的说明串用到当前声明。无本地注释的 override 不自动继承基类文档。
- 非共享组中的注释只用于紧随其后的声明；中间的其他声明（例如 `using`）会消耗该说明，不向后猜测关联。
- 注释不写入白名单；头文件注释变化会使对应类型的增量缓存失效。

`VtkSharp` 项目启用 XML documentation file 输出，NuGet 包包含各目标框架的 XML 文档及 `VTK-LICENSE.txt`。手写 API 的注释由人工维护，生成器不修改手写文件，也不将 `_Internal` 方法注释自动移植到公开的手写包装。无上游说明的 API 允许没有语义注释，暂不检查 CS1591。

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
- 合并时自动补齐新类型的基类链和签名依赖类型；`diff-whitelist` 提前显示这些新增项。无法解析的依赖会阻止合并。
- 手写 partial、runtime helper 和手工 C ABI 导出不进入正式 whitelist。
- 指针返回、引用参数、回调或其他复杂所有权边界必须单独审核；不能因为生成器能够解析就默认 public API 安全。

YamlDotNet 直接反序列化的 DTO 集合应使用 `List<T>`、`Dictionary<TKey, TValue>` 等具体可变类型，避免接口集合无法构造或填充。

## 批量接口规划

优先把最小需求交给 `plan-bindings`，不需要逐类查询、手工拼接 candidate 或逐个补基类。该命令只写候选和报告，不修改正式白名单。

```json
{
  "source": { "kind": "vtk-example", "name": "ExampleName" },
  "requests": [
    { "class": "vtkRenderer", "methods": ["ResetCamera"] },
    { "class": "vtkViewport", "signatures": ["vtkViewport::void SetBackground(double,double,double)"] },
    { "class": "vtkInteractorStyleTerrain", "classOnly": true }
  ]
}
```

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- plan-bindings `
    --requests artifacts/requests.json --output artifacts/candidate.yml --report artifacts/report.json
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist artifacts/candidate.yml --summary
# 审核报告和完整变化后合并
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate artifacts/candidate.yml
```

- `methods` 按 C++ 大小写精确匹配。多重载返回 `ambiguous` 和签名 ID；从报告复制需要的 ID 到 `signatures`，或明确设置 `allOverloads: true` 选择该方法名的全部可直接生成重载。不会按参数个数或类型转换猜测调用。
- `classOnly: true` 与 methods/signatures 互斥，只请求类型；`New()` 仍按头文件自动识别，没有 `New()` 的类型不会被虚构成可创建对象。空 methods/signatures 不代表全量导出。
- 查找沿当前 hierarchy 的单继承链，在最近的同名声明处停止，包括 private/static 声明造成的隐藏；多继承无法确定时返回 `ambiguous`。这不是完整 C++ 调用解析器，不处理 `using` 引入的重载集合或模板实例化语义；这些情况应检查源码并显式请求声明类。
- 每批复用配置、白名单、hierarchy 和 inspection 缓存。已导出的签名不重复加入；新接收类型即使只调用基类方法，也会作为空类型加入候选。
- 状态包括 `ready`、`already-exported`、`needs-metadata`、`unsupported`、`not-found`、`ambiguous`、`invalid-request` 和 `inspection-failed`。已全部导出的重载集合无需再次选择；单个头文件失败会记录原因并继续其他请求。`ready` 仅表示可按现有规则生成，不表示所有权语义已审核。缺失的指针 direction/length、返回值 ownership 不会自动推断。
- 标准输出只显示状态计数和待处理项，详细签名及新增类型写入 JSON 报告。存在未解决项或合并冲突时退出码为 1，但仍写入本批可处理部分的 candidate；必须审查报告，不得把部分成功当作完整导入。输入格式错误则不产出新结果。
- 输入 JSON 字段拼写错误会被拒绝；输入、candidate、report 必须使用不同路径。需求格式见 `schemas/vtksharp.binding-requests.schema.json`。

单个方法的声明类定位无需写需求文件：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- inspect-function vtkRenderer SetBackground --resolve --format json
dotnet run --project src/generator/VtkSharp.Generator.Cli -- create-candidate vtkInteractorStyleTerrain `
    --class-only --supported-only --source-kind manual -o artifacts/type-only.yml
```

不带 `--resolve` 的 `inspect-function` 保持只查当前类的行为。`create-candidate --methods` 保留按方法名选中全部重载的行为；`--supported-only` 现在同时过滤缺少指针元数据的签名，并说明原因。没有可选方法时不会隐式生成空类型，应明确使用 `--class-only`。

参考导出继续使用技能附带的扫描脚本，无需重写解析器。其结果可以直接交给批量规划：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- plan-bindings `
    --reference-scan --requests artifacts/reference-scan.json `
    --output artifacts/candidate.yml --report artifacts/report.json
```

此模式明确按扫描到的方法名选择全部可支持重载，空方法集合只请求类型。扫描警告必须先处理；扫描名称不作为当前 VTK 签名的依据。

## 完整差异与合并边界

`diff-whitelist` 和 `merge-candidate` 共用内存合并及 normalize 结果。diff 包括空类型、自动补齐的基类和签名依赖，原因分别标为 `explicit-request`、`base-of:...` 和 `signature-of:...`；模块随新增类型展示。`--summary` 省略已存在的函数明细，JSON 仍保留计数。

签名按规范化类型比较。candidate 省略的 ownership/direction/length 沿用正式白名单；显式不同值报告冲突，禁止静默覆盖。要修改已有元数据需单独讨论契约变更，不能把添加接口的 merge 当作更新入口。

merge 在写入前校验完整合并结果；有冲突或校验失败时不写正式白名单。通过后只写规范化结果一次，不再先写未规范化版本。写入仍使用既有白名单 writer，不提供跨多个文件的文件系统事务。常规 merge 后无需再运行 normalize；生成检查、native/managed 构建与示例验收仍需执行。
