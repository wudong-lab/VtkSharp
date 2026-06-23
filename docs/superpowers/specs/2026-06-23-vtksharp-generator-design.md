# VtkSharp 绑定生成器设计

## 背景

VtkSharp 的目标是创建有实用价值的 VTK .NET 绑定库，同时实践 AI 工具在工程开发中的协作方式，包括 vibe coding、Codex skills、示例翻译和渐进式 API 补全。

旧项目 `D:\Code\SVN\BrdiVtkNet\src\VtkNetSourceGenerator` 已经验证了一条可行路径：读取 VTK hierarchy 文件和头文件，通过人工维护的 API 白名单生成 C# wrapper 与 C++ C ABI 导出层。当前项目会以此为基础重构生成器，但生成器形态、配置格式和输出规则需要适配 VtkSharp 当前结构。

## 目标

第一阶段实现一个 CLI 优先的 VTK 绑定生成器：

- 通过函数级 YAML 白名单声明需要导出的 VTK API。
- 用 CppAst 解析 VTK 头文件并匹配白名单中的结构化函数签名。
- 生成当前项目风格的 C# 绑定代码和 C++ 导出代码。
- 支持 Codex 调用 CLI 查询候选 API，驱动示例翻译和白名单补全。
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
- 所有被生成 wrapper 的 VTK 类，只要存在 `static New()`，就生成 `New()` 和对应 native 导出。
- `vtkObjectBase`、`vtkObject` 等 `manualBindingClasses` 由人工维护，生成器视为已存在并跳过。
- 生成器优先生成低层稳定绑定；工程友好 API 通过手写 partial 或后续专门规则补充。
- Codex 负责编排分析流程，CLI 提供确定性查询、校验和生成能力。
- 示例翻译产生候选白名单，候选由人工审核后再合并到正式白名单。

## 项目结构

生成器放在 `src/generator` 下：

```text
src/generator/
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
src/bindings/VtkSharp/VtkSharp/
src/native/src/
src/native/vtksharp.modules.generated.cmake
```

`vtksharp.generator.yml` 提交到仓库，保存命名空间、输出路径、白名单目录、native library 名称等公共配置。`vtksharp.generator.local.yml` 不提交，保存本机 VTK 安装路径。也允许通过环境变量覆盖本机路径。

## 生成器形态

生成器分为三个层次：

- `VtkSharp.Generator.Core`：解析、模型、匹配、类型映射、代码生成。
- `VtkSharp.Generator.Cli`：命令行入口，供人工、脚本、Codex 和 CI 调用。
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
vtksharp-gen suggest-api vtkRenderer SetBackground
vtksharp-gen generate --check
vtksharp-gen merge-candidate examples/Cone/whitelist-candidate.yml
```

CLI 做确定性工作：扫描 VTK、输出候选、校验白名单、生成绑定、输出诊断和报告。Codex 负责示例翻译、编译错误分析、调用 CLI 查询候选、生成候选白名单 patch、组织验证流程。

CLI 命令按职责分为三组：

```text
查询类：
  inspect-class
  inspect-function / suggest-api
  list-modules
  list-classes

白名单类：
  validate-whitelist
  normalize-whitelist
  diff-whitelist
  create-candidate

生成类：
  generate
  clean-generated
  report
```

第一阶段优先完成最小可用集，其他命令在示例驱动流程稳定后再补。

## 白名单设计

白名单按 VTK module 拆分：

```text
src/generator/whitelist/
  vtkCommonCore.yml
  vtkCommonDataModel.yml
  vtkRenderingCore.yml
```

每个文件只描述一个 VTK module：

```yaml
module: vtkRenderingCore
classes:
  - name: vtkActor
    header: vtkActor.h
    functions:
      - name: SetMapper
        cppSignature: "void SetMapper(vtkMapper*)"
        returnType: void
        parameters:
          - type: vtkMapper*
            name: mapper
```

函数使用结构化签名匹配：

- `name`
- `returnType`
- `parameters[].type`

参数名不参与匹配，只用于生成 C# API。`cppSignature` 保留 VTK 头文件或 CppAst 看到的原始/近似原始签名，用于人工审核和追溯。若原始签名有形参名，`cppSignature` 应保留形参名；若头文件缺少形参名，则 `cppSignature` 保持缺失状态，`parameters[].name` 由 AI 或人工补齐。

示例：

```yaml
- name: SetPosition
  cppSignature: "void SetPosition(double x, double y, double z)"
  returnType: void
  parameters:
    - { type: double, name: x }
    - { type: double, name: y }
    - { type: double, name: z }
```

无参函数：

```yaml
- name: Update
  cppSignature: "void Update()"
  returnType: void
  parameters: []
```

## 白名单契约

正式白名单是强契约：

- 白名单中声明的类不存在，生成失败。
- 函数结构化签名匹配不到，生成失败。
- 类型不支持，生成失败。
- 依赖 VTK 类无法解析，生成失败。
- 同一结构化签名匹配到多个函数，生成失败。

生成器应输出清晰诊断，便于人工和 Codex 修复。例如列出白名单条目、匹配失败原因、候选函数列表和建议。

可提供探索模式，例如 `--continue-on-error`，用于 AI 批量分析候选 API，但正式生成默认失败即停止。

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

生成器首次发现某个类时创建空壳，之后永不覆盖。C# 文件只包含必要 namespace 和 partial class：

```csharp
namespace VtkSharp;

public unsafe partial class vtkActor { }
```

C++ 文件只包含必要 include：

```cpp
#include "vtksharp_api.h"
#include <vtkActor.h>
```

正式白名单只管理自动生成 API。手写 API 不进入正式白名单。

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
  returnType: void
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
  returnType: void
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
        returnType: void
        parameters:
          - { type: double, name: height }
        reason: "Used by Cone example"
```

流程：

1. Codex 翻译 VTK C++ 示例到 C#。
2. 构建或运行示例。
3. Codex 解析缺失类和缺失成员错误。
4. Codex 调用 `vtksharp-gen inspect-class` 或 `suggest-api` 查询候选签名。
5. Codex 生成 `whitelist-candidate.yml`，不直接修改正式白名单。
6. 人工审核候选白名单。
7. 审核通过后合并到正式模块白名单。
8. 执行 normalize、validate、generate、build 和 smoke test。

正式白名单不记录审核状态。进入正式白名单即视为已审核。

### AI 自动化边界

第一阶段不要求生成器自动解析完整 `dotnet build` 日志并自动修改白名单。推荐先采用 Codex 编排流程：

1. Codex 翻译示例并运行构建。
2. Codex 从编译错误中识别缺失类或成员。
3. Codex 调用 CLI 的 `inspect-class` / `suggest-api` 查询候选 VTK 签名。
4. Codex 生成按示例组织的候选白名单。
5. 人工审核候选后，再合并到正式模块白名单。

后续可以新增 `suggest-from-build-log` 一类命令，把稳定下来的人工流程工具化。但即使有该命令，也只应生成候选白名单或审核报告，不直接修改正式白名单。

## 验证策略

第一阶段采用三层验证：

```bash
vtksharp-gen validate-whitelist
vtksharp-gen generate

cmake --build src/native/out/build/windows-x64 --config Release

dotnet build src/bindings/VtkSharp/VtkSharp.slnx
dotnet run --project examples/Cone/Cone.csproj
```

后续增强：

- `vtksharp-gen generate --check`：临时生成并与当前生成文件 diff，确保生成结果最新。
- native 导出符号检查。
- 引用计数测试。
- 字符串 UTF-8 测试。
- Span 边界和固定长度数组测试。
- 示例 offscreen rendering 截图。
- 图像 hash 或 image diff。

第一阶段示例验证以 smoke test 为主，不做严格图像回归。

## 风险和取舍

- 白名单增长必须人工审核，避免 AI 自动扩大 API 面。
- C++ 指针和 VTK 内部缓存的生命周期不做自动猜测。
- 高级几何语义 API 暂时手写，避免生成器过早复杂化。
- `manualBindingClasses` 允许特殊基础类型绕过生成器，保持主规则简单。
- 生成器默认失败即停止，减少隐性缺失 API。
- CLI 提供确定性能力，Codex 负责探索和编排，这是项目中 AI 协作工程化的核心模式。

## 第一阶段验收标准

- 能读取 YAML 白名单并解析 VTK hierarchy/header。
- 能校验白名单中的类和函数签名。
- 能生成当前项目风格的 C# wrapper 和 C++ export 文件。
- 能跳过 `manualBindingClasses` 中的 `vtkObjectBase`、`vtkObject`。
- 能生成 `vtksharp.modules.generated.cmake`。
- 能创建但不覆盖手写扩展文件。
- 能通过 managed 和 native 构建。
- 能通过至少一个简单 VTK 示例 smoke test。
