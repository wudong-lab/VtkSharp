# VtkSharp 绑定生成器设计

## 背景

VtkSharp 的目标是创建有实用价值的 VTK .NET 绑定库，同时实践 AI 工具在工程开发中的协作方式，包括 vibe coding、AI agent skills、示例翻译和渐进式 API 补全。

旧项目 `D:\Code\SVN\BrdiVtkNet\src\VtkNetSourceGenerator` 已经验证了一条可行路径：读取 VTK hierarchy 文件和头文件，通过人工维护的 API 白名单生成 C# wrapper 与 C++ C ABI 导出层。当前项目会以此为基础重构生成器，但生成器形态、配置格式和输出规则需要适配 VtkSharp 当前结构。

## 目标

第一阶段实现一个 CLI 优先的 VTK 绑定生成器：

- 通过函数级 YAML 白名单声明需要导出的 VTK API。
- 用 CppAst 解析 VTK 头文件并匹配白名单中的结构化函数签名。
- 生成当前项目风格的 C# 绑定代码和 C++ 导出代码。
- 支持 AI 调用 CLI 查询候选 API，驱动示例翻译和白名单补全。
- 保持生成规则简单、稳定、可审核，特殊 API 允许人工补充。

第一阶段不追求全量自动绑定 VTK，也不追求自动理解所有 C++ 类型语义。

## 已确认设计决策摘要

本设计是 VtkSharp 源代码生成器的专项事实来源。若早期总体设计文档中的生成器、manifest 或 API 发现流程与本文冲突，以本文为准。

本轮讨论确认以下原则：

- 采用函数级白名单；类只是组织单位，真正决定是否生成绑定的是具体函数签名。
- 白名单按 VTK module 拆分为 YAML 文件。
- 函数条目同时保留 `cppSignature` 和结构化签名；匹配只使用函数名、返回类型和参数类型，不使用参数名。
- `cppSignature` 尽量保留 VTK 头文件中的原始签名；原始签名有形参名时必须保留。
- `parameters[].name` 始终用于生成 C# API。头文件缺少形参名时，由 AI 或人工补齐，生成器也可按位置生成 `_arg1`、`_arg2` 作为兜底。
- 白名单是强契约，匹配失败、类型不支持或依赖无法解析时默认报错并停止生成。
- 依赖类和基类链由生成器自动发现；依赖类可由 `normalize-whitelist` 写回白名单，通常 `functions: []`。
- generator 只为当前 VTK 类直接声明的 public 实例成员函数生成导出；继承自基类但当前类未重新声明的函数由 C# 继承调用基类 wrapper。
- 所有被生成 wrapper 的 VTK 类，只要存在 `static New()`，就生成 `New()` 和对应 native 导出；其他 static 函数默认忽略，C++ 构造函数和析构函数不导出。
- `vtkObjectBase`、`vtkObject` 等 `manualBindingClasses` 由人工维护，生成器视为已存在并跳过。
- 生成器优先生成低层稳定绑定；工程友好 API 通过手写 partial 或后续专门规则补充。
- AI 负责编排分析流程，CLI 提供确定性查询、校验和生成能力。
- 示例翻译产生候选白名单，候选由人工审核后再合并到正式白名单。

## 项目结构

生成器放在仓库根目录 `generator` 下：

```text
generator/
  VtkSharp.Generator.Core/
  VtkSharp.Generator.Cli/
  VtkSharp.Generator.slnx
  whitelist/
    vtkCommonCore.yml
    vtkRenderingCore.yml
  config/
    vtksharp.generator.yml
    vtksharp.generator.local.yml
```

输出目录：

```text
src/bindings/VtkSharp/
src/native/src/
src/native/vtksharp.modules.generated.cmake
```

`vtksharp.generator.yml` 提交到仓库，保存命名空间、输出路径、白名单目录、native library 名称等公共配置。`vtksharp.generator.local.yml` 不提交，保存本机 VTK 安装路径。也允许通过环境变量覆盖本机路径。

配置分为公共配置和本机配置：

```yaml
# generator/config/vtksharp.generator.yml
vtk:
  version: "9.5"
  modulePrefix: vtk
  runtimeModules:
    - vtkCommonDataModel
    - vtkRenderingOpenGL2
    - vtkInteractionStyle
    - vtkRenderingUI

binding:
  namespace: VtkSharp
  nativeLibraryName: vtksharp_native
  manualBindingClasses:
    - vtkObjectBase
    - vtkObject

paths:
  whitelistDirectory: ../whitelist
  managedOutputDirectory: ../../src/bindings/VtkSharp
  nativeOutputDirectory: ../../src/native/src
  nativeProjectFile: ../../src/native/CMakeLists.txt
  nativeModulesFile: ../../src/native/vtksharp.modules.generated.cmake

generation:
  createManualExtensionFiles: false
  overwriteGeneratedFiles: true
  deleteOrphanGeneratedFiles: false
```

```yaml
# generator/config/vtksharp.generator.local.yml
vtk:
  rootDirectory: "C:/Program Files/VTK"
```

路径解析规则：

- 公共配置中的相对路径，以 `vtksharp.generator.yml` 所在目录为基准。
- local 配置只保存机器相关路径，不提交到仓库。
- `vtk.includeDirectory` 和 `vtk.hierarchyDirectory` 默认由 `rootDirectory + version` 推导，但允许 CLI、环境变量或 local 配置显式覆盖。

配置优先级：

```text
CLI 参数
> 环境变量
> vtksharp.generator.local.yml
> vtksharp.generator.yml
> 默认值
```

环境变量 `VTK_ROOT` 可覆盖 `vtk.rootDirectory`。

## 生成器形态

生成器分为三个层次：

- `VtkSharp.Generator.Core`：解析、模型、匹配、类型映射、代码生成。
- `VtkSharp.Generator.Cli`：命令行入口，供人工、脚本、AI 和 CI 调用。
- 未来可选 `VtkSharp.Generator.App`：基于 Avalonia 的白名单浏览和审核 UI。

第一阶段只实现 Core 和 CLI，不引入 WPF/DevExpress。

推荐技术栈：

- CppAst：解析 VTK headers。
- YamlDotNet：读取和写入 YAML 白名单。
- System.CommandLine：实现 CLI。
- StringBuilder 或小型模板：生成代码。输出规则稳定后再考虑 Scriban。

## CLI 命令

第一阶段最小命令集：

```bash
vtksharp-gen inspect-class vtkActor
vtksharp-gen validate-whitelist
vtksharp-gen normalize-whitelist
vtksharp-gen generate
```

后续可扩展：

```bash
vtksharp-gen list-modules
vtksharp-gen list-classes
vtksharp-gen inspect-function vtkRenderer SetBackground
vtksharp-gen generate-bindings --check
vtksharp-gen merge-candidate examples/Cone/whitelist-candidate.yml
```

CLI 做确定性工作：扫描 VTK、输出候选、校验白名单、生成绑定、输出诊断和报告。AI 负责示例翻译、编译错误分析、调用 CLI 查询候选、生成候选白名单 patch、组织验证流程。

CLI 命令按职责分为三组：

```text
查询类：
  inspect-class
  inspect-function
  list-modules
  list-classes

白名单类：
  validate-whitelist
  normalize-whitelist
  diff-whitelist
  create-candidate
  merge-candidate

生成类：
  generate-bindings
```

第一阶段优先完成最小可用集，其他命令在示例驱动流程稳定后再补。

第一阶段命令行为：

`validate-whitelist`：

- 读取公共配置、local 配置和覆盖参数。
- 读取所有模块白名单。
- 解析 VTK hierarchy。
- 解析白名单涉及的 headers。
- 校验 module、class、header 是否一致。
- 校验函数结构化签名能唯一匹配。
- 校验类型都受支持，或已在白名单中补齐必要元数据。
- 不写任何输出文件。

`inspect-class`：

- 解析指定 VTK 类。
- 输出 public 候选函数列表。
- 支持 `--format text|json`。
- 输出 canonical signature、`cppSignature`、参数名、返回类型、是否支持自动生成、依赖类。

`normalize-whitelist`：

- 排序模块、类和函数。
- 补齐依赖类和基类链。
- 标准化 type 字符串。
- 可更新 `cppSignature` 的格式，但不覆盖人工参数名。
- 写回白名单文件，必须通过 Git diff 供人工审核。

`generate`：

- 默认先执行 `validate-whitelist`。
- 覆盖 `*_gen.cs` 和 `*_export_gen.cpp`。
- 第一阶段不默认创建 `*.cs` 和 `*_export.cpp` 扩展空壳。
- 生成 `vtksharp.modules.generated.cmake`。
- 不修改白名单。
- 第一阶段不删除 orphan generated files。

`generate --check` 和 orphan 清理放到后续增强，不作为第一版必须项。

## 白名单设计

白名单按 VTK module 拆分：

```text
generator/whitelist/
  vtkCommonCore.yml
  vtkCommonDataModel.yml
  vtkRenderingCore.yml
```

每个文件只描述一个 VTK module：

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json

module: vtkRenderingCore
classes:
  - name: vtkActor
    header: vtkActor.h
    functions:
      - name: SetMapper
        cppSignature: "void SetMapper(vtkMapper* mapper)"
        return:
          type: void
        parameters:
          - type: vtkMapper*
            name: mapper
```

函数使用结构化签名匹配：

- `name`
- `return.type`
- `parameters[].type`

参数名不参与匹配，只用于生成 C# API。`cppSignature` 保留 VTK 头文件或 CppAst 看到的原始/近似原始签名，用于人工审核和追溯。若原始签名有形参名，`cppSignature` 应保留形参名；若头文件缺少形参名，则 `cppSignature` 保持缺失状态，`parameters[].name` 由 AI 或人工补齐。

示例：

```yaml
- name: SetPosition
  cppSignature: "void SetPosition(double x, double y, double z)"
  return:
    type: void
  parameters:
    - { type: double, name: x }
    - { type: double, name: y }
    - { type: double, name: z }
```

无参函数：

```yaml
- name: Update
  cppSignature: "void Update()"
  return:
    type: void
  parameters: []
```

返回 VTK 对象：

```yaml
- name: GetProperty
  cppSignature: "vtkProperty* GetProperty()"
  return:
    type: vtkProperty*
  parameters: []
```

返回值使用对象式 schema。`return.type` 第一阶段必须有，后续可以扩展所有权等信息：

```yaml
return:
  type: vtkLight*
  ownership: owned
```

字段约束：

- `module`：必须有，值为 `vtkRenderingCore` 这种 VTK module 名。
- `classes[].name`：必须有。
- `classes[].header`：必须有，便于不用每次从 hierarchy 推断。
- `classes[].functions`：必须有，可以为空数组。
- `functions[].name`：必须有。
- `functions[].cppSignature`：必须有，用于审核；若原始函数签名中有形参名，必须保留形参名。
- `functions[].return.type`：必须有。
- `functions[].parameters`：必须有，可以为空数组。
- `parameters[].type`：必须有。
- `parameters[].name`：必须有。
- `parameters[].direction`：普通标量和 `vtkClass*` 可省略，指针/数组需要时填写。
- `parameters[].length`：仅 `T*` 或数组参数需要。
- 不在正式白名单里放 `status/reviewStatus/reason`。

正式白名单、候选白名单和生成器配置都应配套 JSON Schema，便于 VS Code 通过 YAML Language Server 自动补全和合法性校验：

```text
generator/schemas/
  vtksharp.whitelist.schema.json
  vtksharp.whitelist-candidate.schema.json
  vtksharp.generator.schema.json
  vtksharp.generator.local.schema.json
```

JSON Schema 应约束必填字段、枚举值和未知字段。例如 `direction` 只允许 `in | out | inout`，`length.kind` 只允许 `fixed | parameter`。

## 白名单契约

正式白名单是强契约：

- 白名单中声明的类不存在，生成失败。
- 函数结构化签名匹配不到，生成失败。
- 类型不支持，生成失败。
- 依赖 VTK 类无法解析，生成失败。
- 同一结构化签名匹配到多个函数，生成失败。

生成器应输出清晰诊断，便于人工和 AI 修复。例如列出白名单条目、匹配失败原因、候选函数列表和建议。

可提供探索模式，例如 `--continue-on-error`，用于 AI 批量分析候选 API，但正式生成默认失败即停止。

## VTK 函数导出规则

- 候选函数列表、白名单校验和最终生成必须使用一致的可导出函数规则。
- generator 只为当前 VTK 类直接声明的 public 实例成员函数生成导出；继承自基类但当前类未重新声明的函数，不在当前类重复导出。
- C# 端通过继承调用基类 wrapper 时，语义应等价于 C++ 通过基类指针/引用调用；是否分派到子类实现由 C++ virtual dispatch 决定。
- 如果当前类声明了同名函数，应按 C++ 名称隐藏规则理解候选函数，不应把被隐藏的基类 overload 当作当前类可直接调用的函数。
- VTK 静态函数中只特殊导出 `static New()`；其他 static 函数默认忽略。
- C++ 构造函数、析构函数不导出。
- 这些规则应在 inspect/candidate、validate 和 emit 三个阶段保持一致，避免候选列表或白名单校验放行后 native 编译失败。

## 依赖类和规范化

白名单只需要声明真正想开放的函数。生成器会自动发现：

- 参数类型中的 `vtkClass*`
- 返回值中的 `vtkClass*`
- 继承链上的基类

依赖类会被生成 wrapper 壳，但不会自动导出它的成员函数。依赖类可由 `normalize-whitelist` 写回对应模块白名单，下次扫描时成为显式类，通常 `functions: []`。

`generate` 命令不应静默修改正式白名单。规范化、排序、补齐依赖类、补齐基类链等会改变白名单内容的操作应由 `normalize-whitelist` 或专门命令执行，并通过 Git diff 供人工审核。

凡是生成器解析到并生成 wrapper 的 VTK 类，只要存在 `static New()`，就生成 `New()` 和对应 native 导出。

## C ABI 导出命名

C++ 支持函数重载，但 C ABI 导出函数名必须唯一。生成器应自动确定 P/Invoke 导出函数名，不要求人工指定。

基础规则：

1. 同一类中同名导出函数只有一个时，导出名为 `ClassName_MethodName`。
2. 同一类中同名导出函数存在多个重载时，导出名为 `ClassName_MethodName_NormalizedParameterTypeList`。
3. 返回值不参与常规导出名。C++ 不能仅按返回值重载；返回值只参与冲突兜底 hash。
4. 参数名不参与导出名。
5. 如果规范化后的导出名仍冲突，追加稳定短 hash。

示例：

```text
vtkActor::SetMapper(vtkMapper*)
  -> vtkActor_SetMapper

vtkActor::SetPosition(double x, double y, double z)
  -> vtkActor_SetPosition_double_double_double

vtkActor::SetPosition(double position[3])
  -> vtkActor_SetPosition_doubleArray3

vtkTransform::SetMatrix(vtkMatrix4x4*)
  -> vtkTransform_SetMatrix_vtkMatrix4x4Ptr

vtkTransform::SetMatrix(double const[16])
  -> vtkTransform_SetMatrix_doubleConstArray16
```

参数类型后缀由 canonical type 生成。第一阶段推荐规范化规则：

| C++ parameter type | suffix |
| --- | --- |
| `double` | `double` |
| `float` | `float` |
| `int` | `int` |
| `unsigned int` | `uint` |
| `long long` | `long` |
| `unsigned long long` | `ulong` |
| `vtkIdType` | `vtkIdType` |
| `bool` | `bool` |
| `vtkTypeBool` | `vtkTypeBool` |
| `vtkMapper*` | `vtkMapperPtr` |
| `vtkMapper const*` / `const vtkMapper*` | `vtkMapperConstPtr` |
| `const char*` / `char const*` | `constCharPtr` |
| `char*` | `charPtr` |
| `void*` | `voidPtr` |
| `double[3]` | `doubleArray3` |
| `double const[3]` / `const double[3]` | `doubleConstArray3` |
| `int[2]` | `intArray2` |
| `HWND__*` | `hwnd` |
| `HDC__*` | `hdc` |
| `HGLRC__*` | `hglrc` |

非固定长度 `T*` 参数若通过白名单声明了 `length.kind: fixed`，可使用 `TArrayN` 后缀；否则使用 `TPtr` 后缀。方向 `in` / `out` / `inout` 不参与后缀，除非不参与会导致冲突。

稳定 hash 只在冲突时追加，格式建议为 `_hxxxxxx`，例如：

```text
vtkFoo_Bar_intPtr_h1a2b3c
```

hash 输入使用 canonical signature，至少包含：

```text
class name
method name
return canonical type
parameter canonical types
const/ref/pointer/array/fixed-length 信息
```

hash 输入不包含参数名和头文件中的声明顺序。禁止使用 CppAst 函数索引号作为重载命名后缀，因为索引会随 VTK 版本、头文件顺序和过滤规则变化而变化。

## CppAst 类型规范化

生成器内部应建立 `TypeCanonicalizer`，把 CppAst 读到的不同 C++ 类型写法统一成白名单使用的 canonical type 字符串。白名单匹配只使用 canonical type，不直接使用 CppAst 原始 `FullName`。

白名单 canonical type 采用紧凑 C++ 风格：

```text
void
bool
vtkTypeBool
int
unsigned int
long long
unsigned long long
vtkIdType
float
double
vtkMapper*
const vtkMapper*
const char*
char*
void*
double[3]
const double[3]
int[2]
HWND
HDC
HGLRC
```

规范化规则：

| CppAst / C++ spelling | canonical type |
| --- | --- |
| `vtkMapper *` | `vtkMapper*` |
| `vtkMapper*` | `vtkMapper*` |
| `vtkMapper const *` | `const vtkMapper*` |
| `const vtkMapper *` | `const vtkMapper*` |
| `char const *` | `const char*` |
| `const char *` | `const char*` |
| `double const[3]` | `const double[3]` |
| `const double[3]` | `const double[3]` |
| `double [3]` | `double[3]` |
| `HWND__ *` | `HWND` |
| `HDC__ *` | `HDC` |
| `HGLRC__ *` | `HGLRC` |

参数名、空格和 `const` 位置不影响匹配。数组统一为 `T[N]` 或 `const T[N]`。指针统一为 `T*` 或 `const T*`。

`unsigned long` 不默认映射为 `uint`，因为它的平台宽度不稳定。若某个 API 必须支持 `unsigned long`，应通过配置或显式类型别名处理，例如：

```yaml
typeAliases:
  unsigned long:
    windows-x64: uint
```

第一阶段如果遇到没有明确映射的 `unsigned long`，生成器应报错或要求人工决策，而不是隐式假定为 32 位。

> **2026-06-25 决策**：`typeAliases` YAML 配置机制暂不实现。理由：
> 1. 当前场景只面向 Windows（MSVC），`unsigned long` 恒为 32-bit，不存在平台宽度分歧。
> 2. VTK 9.5 公共 API 中 `unsigned long` 极少出现（VTK 用 `vtkIdType` 做索引，用 `int`/`unsigned int` 做标志位）。
> 3. `WhitelistValidator` 遇到不支持类型时已正确报错，报错本身就驱动人工显式确认。
>
> 等实际遇到需要 `unsigned long` 的 API 时再评估是否只需在对应白名单条目中写 `uint` 即可，无需引入配置层的抽象。

## 手写基础类

生成器支持 `manualBindingClasses` 配置：

```yaml
manualBindingClasses:
  - vtkObjectBase
  - vtkObject
```

这些类由人工维护，生成器应忽略：

- 不生成 `*_gen.cs`。
- 不生成 `*_export_gen.cpp`。
- 不创建 `*.cs` 或 `*_export.cpp` 占位。
- 依赖解析遇到这些类时视为已存在、可引用。
- 继承链可以止于这些类。

后续如果遇到 `vtkCommand`、`vtkCallbackCommand`、事件回调、特殊集合类型等，也可以加入 `manualBindingClasses`，避免为了少数特殊基础类型让生成器规则过度复杂。

## 输出文件治理

自动生成文件：

```text
*_gen.cs
*_export_gen.cpp
```

这些文件每次生成时可覆盖，不允许人工修改。文件头包含：

```csharp
// <auto-generated/>
// This file is generated by VtkSharp.Generator. Do not edit manually.
```

手写扩展文件：

```text
*.cs
*_export.cpp
```

第一阶段不默认创建手写扩展空壳。除 `manualBindingClasses` 等特殊类型外，自动生成的 VTK wrapper 类只生成 `vtkXxx_gen.cs` 和 `vtkXxx_export_gen.cpp`。C# 侧 `vtkXxx.cs` 与 native 侧 `vtkXxx_export.cpp` 的自动创建都放到后续阶段。

成熟版本的生成器应支持自动创建扩展空壳：凡是生成器自动添加/生成 wrapper 的类型，都可以在文件不存在时创建对应的 `vtkXxx.cs` 和 `vtkXxx_export.cpp`。已存在的扩展文件永不覆盖。

C# 扩展空壳只包含必要 namespace 和 partial class：

```csharp
namespace VtkSharp;

public unsafe partial class vtkActor { }
```

C++ 扩展空壳只包含必要 include：

```cpp
#include "vtksharp_api.h"
#include <vtkActor.h>
```

正式白名单只管理自动生成 API。手写 API 不进入正式白名单。

### C# 生成代码布局

C# `*_gen.cs` 的代码布局以当前项目中的 `vtkAlgorithm_gen.cs` 为基准。其他手工编辑过的 `.cs` 文件可能存在布局差异，不作为生成器格式参考。

推荐顺序：

```text
// <auto-generated/>
using ...

namespace VtkSharp;

public unsafe partial class vtkXxx : vtkBase
{
    protected constructor
    static New / WeakReference / Register
    public wrapper methods

    #region Interop
    private P/Invoke declarations
    #endregion
}
```

P/Invoke 声明必须集中放在类末尾的 `#region Interop` / `#endregion` 中。public wrapper 方法应位于 interop region 之前。这样生成代码的可读性和当前项目已有布局保持一致。

## 类型映射原则

生成器优先生成低层、稳定、接近 ABI 的绑定。工程友好的高级 API 通过 partial 手写或后续专门规则提供。

第一阶段支持：

| C++ type | C# import type | C# public wrapper |
| --- | --- | --- |
| `void` | `void` | `void` |
| `bool` | `bool` + `[MarshalAs(UnmanagedType.U1)]` | `bool` |
| `vtkTypeBool` | `bool` + `[MarshalAs(UnmanagedType.U4)]` | `bool` |
| `char` | `char` | `char` |
| `int` | `int` | `int` |
| `unsigned int` | `uint` | `uint` |
| `long long` | `long` | `long` |
| `unsigned long long` | `ulong` | `ulong` |
| `vtkIdType` | `long` | `long` |
| `float` | `float` | `float` |
| `double` | `double` | `double` |
| `vtkClass*` | `nint` | `vtkClass` wrapper |
| `const char*` / `char const*` 参数 | TFM 条件导入，见字符串规则 | `string` 参数 |
| `char*` / `const char*` 返回值 | `nint` | `string`，通过 `VtkString.FromUtf8Pointer(...)` 解码 |
| `void*` | `nint` | `nint` |
| `HWND` / `HDC` / `HGLRC` | `nint` | `nint` |
| `T[N]` 参数 | pointer 或 TFM 条件导入 | `Span<T>` / `ReadOnlySpan<T>` |
| `T*` 返回值，非 `vtkClass*` / 字符串 | `T*` | `internal *_Internal()` |

不支持类型默认报错。

### 字符串

项目 multi-target：

```xml
netstandard2.0;net10.0
```

生成器按 TFM 条件生成互操作代码：

- `net10.0+`：`LibraryImport` + `StringMarshalling.Utf8`。
- `netstandard2.0`：`DllImport` + 显式 UTF-8 helper。

字符串参数：

```csharp
public void SetName(string name)
{
#if NET10_0_OR_GREATER
    vtkObject_SetName(this.NativePointer, name);
#else
    vtkObject_SetName(this.NativePointer, VtkString.ToNullTerminatedUtf8(name));
#endif
}
```

字符串返回值统一保守处理：

- native import 返回 `nint`。
- public wrapper 使用 `VtkString.FromUtf8Pointer(...)` 解码。

`VtkString` 作为手写 runtime helper 维护，不由每个生成文件重复生成。

### 数组和指针参数

固定长度数组自动支持：

- `double const[3]` / `const double[3]`：`ReadOnlySpan<double>`。
- `double[3]`：`Span<double>`。

非固定长度指针必须由白名单人工补齐元数据：

```yaml
- name: SetArray
  cppSignature: "void SetArray(double* values, vtkIdType count)"
  return:
    type: void
  parameters:
    - type: double*
      name: values
      direction: in
      length:
        kind: parameter
        name: count
    - type: vtkIdType
      name: count
```

或者：

```yaml
- name: SetPosition
  cppSignature: "void SetPosition(double* position)"
  return:
    type: void
  parameters:
    - type: double*
      name: position
      direction: in
      length:
        kind: fixed
        value: 3
```

没有 `direction + length` 的 `T*` 参数默认不支持。

### 返回指针

返回规则：

```text
vtkClass* return
  -> public vtkClass Method()
  -> vtkClass.WeakReference(nativePtr)

char* / const char* return
  -> public string Method()
  -> VtkString.FromUtf8Pointer(nativePtr)

double* / float* / int* return
  -> internal unsafe T* Method_Internal()
  -> 不自动生成 public 友好 API

void* return
  -> nint
```

基础类型指针返回值的生命周期和语义复杂，第一阶段不自动包装成 `Point3D`、`Color`、数组或 Span。需要时在手写 partial 中补充。

## CMake 集成

主 `CMakeLists.txt` 保持稳定，只接入一次 generated cmake 片段：

```cmake
include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)
```

生成器维护：

```cmake
set(VTKSHARP_VTK_COMPONENTS
  CommonCore
  RenderingCore
)

set(VTKSHARP_VTK_TARGETS
  VTK::CommonCore
  VTK::RenderingCore
)
```

主 CMake 使用：

```cmake
find_package(VTK CONFIG REQUIRED COMPONENTS
  ${VTKSHARP_VTK_COMPONENTS}
)

target_link_libraries(vtksharp_native
  PRIVATE
    ${VTKSHARP_VTK_TARGETS}
)

vtk_module_autoinit(
  TARGETS vtksharp_native
  MODULES
    ${VTKSHARP_VTK_TARGETS}
)
```

生成器从正式白名单和依赖类集合收集 VTK modules，并将 `vtkRenderingCore` 映射为 `RenderingCore` 与 `VTK::RenderingCore`。第一阶段可使用去掉 `vtk` 前缀的规则，遇到不符合规则的模块再报错或通过配置显式映射。

C++ 源文件继续由 `file(GLOB_RECURSE ...)` 收集，生成器不生成源文件列表。

## 示例驱动白名单补全

候选白名单按示例组织：

```text
examples/
  Cone/
    Cone.cs
    whitelist-candidate.yml
    porting-notes.md
```

候选格式：

```yaml
status: proposed
source:
  kind: vtk-example
  name: Cone
  original: "Examples/GeometricObjects/Cxx/Cone.cxx"

requirements:
  - module: vtkFiltersSources
    class: vtkConeSource
    header: vtkConeSource.h
    functions:
      - name: SetHeight
        cppSignature: "void SetHeight(double _arg)"
        return:
          type: void
        parameters:
          - { type: double, name: height }
        reason: "Used by Cone example"
```

流程：

1. AI 翻译 VTK C++ 示例到 C#。
2. 构建或运行示例。
3. AI 解析缺失类和缺失成员错误。
4. AI 调用 `vtksharp-gen inspect-class` 或 `inspect-function` 查询候选签名。
5. AI 生成 `whitelist-candidate.yml`，不直接修改正式白名单。
6. 人工审核候选白名单。
7. 审核通过后合并到正式模块白名单。
8. 执行 normalize、validate、generate-bindings、build 和 smoke test。

正式白名单不记录审核状态。进入正式白名单即视为已审核。

### AI 自动化边界

第一阶段不要求生成器自动解析完整 `dotnet build` 日志并自动修改白名单。推荐先采用 AI 编排流程：

1. AI 翻译示例并运行构建。
2. AI 从编译错误中识别缺失类或成员。
3. AI 调用 CLI 的 `inspect-class` / `inspect-function` 查询候选 VTK 签名。
4. AI 生成按示例组织的候选白名单。
5. 人工审核候选后，再合并到正式模块白名单。

后续可以新增 `suggest-from-build-log` 一类命令，把稳定下来的人工流程工具化。但即使有该命令，也只应生成候选白名单或审核报告，不直接修改正式白名单。

## 验证策略

第一阶段采用三层验证：

```bash
vtksharp-gen validate-whitelist
vtksharp-gen generate-bindings --check

cmake --build src/native/out/build/windows-x64 --config Release

dotnet build src/bindings/VtkSharp.slnx
dotnet run --project src/bindings/TestConsole/TestConsole.csproj -- --smoke
```

后续增强：

- `vtksharp-gen generate --check`：临时生成并与当前生成文件 diff，确保生成结果最新。
- CppAst 解析性能优化：当前第一阶段可以接受按类/按 header 逐次解析，优先打通 validate、generate、round-trip diff、build 和 smoke test 的整体闭环；后续应改为对本轮需要分析的类进行批量解析，并缓存 header/module 解析结果，避免 `validate-whitelist` 和 `generate` 重复解析同一批 VTK 头文件。
- native 导出符号检查。
- 引用计数测试。
- 字符串 UTF-8 测试。
- Span 边界和固定长度数组测试。
- 示例 offscreen rendering 截图。
- 图像 hash 或 image diff。

第一阶段示例验证以 smoke test 为主，不做严格图像回归。

## 初始白名单来源

第一版白名单不从零手写，也不直接导入旧 BrdiVtkNet 的全量白名单。初始白名单应从当前 VtkSharp 已有绑定代码反推，优先实现当前代码的 round-trip：

```text
src/bindings/VtkSharp/**/*_gen.cs
src/native/src/**/*_export_gen.cpp
```

初始流程：

1. 读取当前已有 C# 和 C++ 绑定样本。
2. 结合 VTK headers 和旧项目 `vtkExportConfig.json` 辅助确认函数签名。
3. 整理当前项目已有类和函数对应的最小白名单。
4. 先生成到临时目录。
5. 与当前已有代码做人工 diff。
6. 逐步让生成结果接近当前项目风格。
7. round-trip 稳定后再切换到正式输出目录。

第一阶段不导入旧项目 110 个类 / 545 个函数的完整覆盖范围。旧白名单只能作为签名和迁移参考，不能作为第一版自动生成范围。

第一阶段类型和 API 支持范围也以当前已有绑定代码为边界。若反推白名单或生成过程中遇到当前代码之外的类型规则，先报错并记录为后续示例驱动扩展项，不在第一版中临时扩大规则。

当前已有代码已经可以跑通一个完整 VTK 程序，验证样本位于：

```text
src/bindings/TestConsole
```

`TestConsole` 当前覆盖了 `vtkConeSource`、`vtkPolyDataMapper`、`vtkActor`、`vtkRenderer`、`vtkRenderWindow` 和 `vtkRenderWindowInteractor` 的基础 pipeline，可作为第一阶段 round-trip 后的 smoke test 样本。

## 风险和取舍

- 白名单增长必须人工审核，避免 AI 自动扩大 API 面。
- C++ 指针和 VTK 内部缓存的生命周期不做自动猜测。
- 高级几何语义 API 暂时手写，避免生成器过早复杂化。
- `manualBindingClasses` 允许特殊基础类型绕过生成器，保持主规则简单。
- 生成器默认失败即停止，减少隐性缺失 API。
- CLI 提供确定性能力，AI 负责探索和编排，这是项目中 AI 协作工程化的核心模式。
- CppAst 解析 VTK header 成本较高。第一阶段不为性能优化打断整体流程，允许 CLI 较慢；一旦 round-trip 和 smoke test 稳定，应优先引入批量解析、解析结果缓存和跨命令复用的 inspection model。

## 第一阶段验收标准

- 能读取 YAML 白名单并解析 VTK hierarchy/header。
- 能校验白名单中的类和函数签名。
- 能生成当前项目风格的 C# wrapper 和 C++ export 文件。
- 能跳过 `manualBindingClasses` 中的 `vtkObjectBase`、`vtkObject`。
- 能生成 `vtksharp.modules.generated.cmake`。
- 第一阶段不自动创建手写扩展空壳；如果扩展文件已存在，生成器不覆盖。
- 能通过 managed 和 native 构建。
- 能通过至少一个简单 VTK 示例 smoke test。

第一阶段实现验收以“临时目录生成 + 人工 diff + 当前已有 API round-trip”为主：

```bash
vtksharp-gen validate-whitelist
vtksharp-gen inspect-class vtkActor --format text
vtksharp-gen generate --output-root <temp-dir>
```

第一版 `generate` 应支持将生成结果输出到临时目录，避免一开始覆盖当前项目已有绑定代码。待 round-trip 结果稳定后，再使用正式输出路径。

第一阶段不要求：

- 自动解析 build log。
- 自动翻译示例。
- `generate --check`。
- 删除 orphan generated files。
- 支持旧 BrdiVtkNet 全量白名单。
- 支持所有复杂 pointer / ownership 规则。
- 完整 CI。
