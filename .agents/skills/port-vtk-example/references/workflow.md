# VTK 示例翻译工作流

## 目录

1. 获取和分析源码
2. 翻译 C# 示例
3. 发现缺失绑定
4. 创建并合并 candidate
5. 生成和构建
6. 验证与记录

## 1. 获取和分析源码

优先顺序：

1. 用户给出本地路径时直接读取。
2. 用户给出 VTK 示例 URL 时下载页面或对应源码。
3. 仅给出示例名时查找官方实际源码。

提取 examples.vtk.org 的 Description 时应允许 `<h3>` 携带属性和内部锚点；页面没有 Description 时不要虚构说明。

列出源码使用的所有 VTK 类型、直接调用的方法、事件、回调以及可能无法生成绑定的复杂签名。

## 2. 翻译 C# 示例

在 `src/examples/ExampleBrowser/Examples/<Category>/<Name>/` 创建 `<Name>.cs`，沿用邻近示例的命名空间、`IExample` 和 `[Example]` 写法。

常用转换规则：

- `vtkNew<vtkXxx>` 转为 `using var value = vtkXxx.New();`。
- VTK wrapper 明确释放，避免依赖终结器。
- `std::cout`、`printf` 转为 `Debug.WriteLine`。
- 使用现有 `vtkNamedColors` 和值类型辅助 API，不重新实现颜色转换。
- 事件常量使用 `vtkCommand.<EventName>`。
- C++ 的 `vtkCallbackCommand` 转为 managed `AddObserver`；需要上下文时使用带 `clientData` 的 overload，并确保 observer 生命周期覆盖回调期间。

在 `Run()` 开头保留简短的原始说明和源码 URL。若遇到非 VTK 对象指针、引用输出、复杂 STL 类型等不支持签名，优先在示例侧采用清晰替代方案。

## 3. 发现缺失绑定

先构建：

```powershell
dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj
```

从编译错误收集缺失类型和成员。成员可能声明在基类上，应使用：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    inspect-function <ClassName> <MethodName> --format json
```

检查新类型的完整基类链是否已有 wrapper。

## 4. 创建并合并 candidate

对每个声明类只选择示例实际需要的方法：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    create-candidate <ClassName> `
    -o <temporary-candidate.yml> `
    --supported-only `
    --source-kind vtk-example `
    --source-name <Name> `
    --source-original <source> `
    --methods Method1 Method2
```

不传 `--methods` 会包含该类所有可导出方法，因此不能用它表达“仅创建空类型”。`New()` 由生成器识别，不列入普通方法。

合并前必须检查：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist <candidate.yml>
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate <candidate.yml>
dotnet run --project src/generator/VtkSharp.Generator.Cli -- validate-whitelist
```

若方法不属于传入类型，返回到声明类检查，不直接编辑正式 whitelist 绕过校验。

## 5. 生成和构建

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    generate-bindings --output-root src --incremental

$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.6"
.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir
dotnet test src/bindings/VtkSharp.slnx --configuration Release
dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release
```

生成代码编译失败时修正 whitelist、类型映射或 generator；不要直接修改 `*_gen.cs` 和 `*_export_gen.cpp`。

## 6. 验证与记录

运行 ExampleBrowser，选择目标示例并确认首帧、相机、交互和关闭释放正常。然后执行：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

在示例目录维护 `porting-notes.md`，记录原始源码、使用类型、新增 API、无法等价翻译的签名和实际验证结果。最终审查 `git diff`，确认没有无关 whitelist 扩张或生成文件手改。
