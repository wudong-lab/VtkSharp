# VTK 示例翻译工作流

## 源码与 C# 翻译

用户给出本地路径时读取文件；给出 URL 或示例名时获取官方实际 C++ 源码和说明。提取 examples.vtk.org 的 Description 时允许 h3 带属性及内部锚点；没有说明时不要虚构。

在 `src/examples/ExampleBrowser/Examples/<Category>/<Name>/` 沿用当前 `IExample`、`ExampleAttribute` 和邻近示例写法，在 Run 开头保留简短原始说明及源码 URL。

- `vtkNew<vtkXxx>` 转为 `using var value = vtkXxx.New();`，明确释放 wrapper。
- `std::cout`、`printf` 转为 `Debug.WriteLine`。
- 沿用现有颜色和值类型辅助 API，事件常量使用 `vtkCommand.<EventName>`。
- `vtkCallbackCommand` 转为 managed `AddObserver`；需要上下文时使用 clientData overload，并确保 observer 生命周期覆盖回调期间。
- 对不支持的签名采用明确替代方案，说明等价性和性能影响。

## 缺失绑定与批量规划

先构建 ExampleBrowser，从编译错误收集缺失类型和成员。将最小需求写入临时 JSON：

```json
{
  "source": { "kind": "vtk-example", "name": "ExampleName" },
  "requests": [
    { "class": "vtkRenderer", "methods": ["ResetCamera"] },
    { "class": "vtkInteractorStyleTerrain", "classOnly": true }
  ]
}
```

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- plan-bindings `
    --requests <temporary-requests.json> --output <temporary-candidate.yml> --report <temporary-report.json>
dotnet run --project src/generator/VtkSharp.Generator.Cli -- diff-whitelist <temporary-candidate.yml> --summary
```

CLI 负责定位声明类、判断已导出签名和组织 candidate。不要先逐类 inspect，也不要人工检查完整基类链。`ambiguous` 从报告选取实际需要的签名 ID，放入 `signatures` 后重跑；示例不默认选择全部重载。单个问题可用 `inspect-function <Class> <Method> --resolve --format json`。

`classOnly` 只补类型，New 自动识别；不要为满足工具限制而添加无关方法。`needs-metadata` 的指针长度、方向及所有权仍需查原生契约。多继承、using 引入重载及模板语义需检查源码，不当作普通继承自动处理。

规划遇到未解决项会返回 1，仍生成可处理部分的 candidate 和报告；先决定未解决项的处理方式，不将部分结果误认为完成。详细输入格式见 `docs/generator.md` 的“批量接口规划”。

审核完整 diff 后执行 `merge-candidate`；它会校验结果、补齐基类和签名依赖并规范化。单类 `create-candidate` 仍可用，但必须加 `--supported-only`；只补类型时使用 `--class-only`。

## 构建、运行与记录

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate <temporary-candidate.yml>
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src --incremental

# 确认这是当前安装中包含 VTKConfig.cmake 的目录
$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.7"
.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir
dotnet test src/bindings/VtkSharp.slnx --configuration Release
dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release
```

生成代码编译失败时修正 whitelist 元数据、类型映射或 generator，不手改生成文件。

运行目标示例，确认首帧、相机、交互与关闭释放，然后执行 `generate-bindings --check`。在示例目录维护 `porting-notes.md`：引用原始源码，记录翻译差异、排除项和实际验证结果；新增 API 与依赖清单优先来自工具报告。最后审查 git diff，确认没有无关扩张或生成代码手改。
