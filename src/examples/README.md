# VtkSharp 示例

`ExampleBrowser` 是 Windows x64 的 WPF 示例浏览器，目标框架为 `net8.0-windows`。
可浏览源码并运行几何对象、建模、网格、图像、交互和背景等示例；`ExtraExamples` 提供 WPF 承载与回调演示。

## 运行

先按 [README](../../README.md) 安装工具链、构建 VTK 并配置环境变量。在仓库根目录执行：

```powershell
.\tools\build-all.ps1 -Configuration Release
dotnet run --project src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release
```

选择 `GeometricObjects / Cone` 并运行，应出现可交互的圆锥窗口。
`dotnet run` 会构建托管项目，但不会构建 native DLL；Debug 运行需要另行构建 Debug VTK 与 native。
示例需 .NET 8 Desktop Runtime 及可用图形环境。自动截图验收见 [验证文档](../../docs/workflow/verification.md)。

## 目录约定

```text
ExampleBrowser/
├── Examples/<Category>/<ExampleName>/
│   ├── <ExampleName>.cs     # IExample 实现
│   ├── candidate.yml       # 有绑定补充时保留候选
│   ├── porting-notes.md    # 上游来源、移植差异和验证记录
│   └── Data/              # 示例需要时提供数据
└── ExtraExamples/         # 项目特有的宿主集成与辅助接口示例
```

## 添加示例

1. 从 VTK 源码的 `Examples/`、VTK 示例网站或其他有明确许可的上游获取 C++ 示例，并记录来源。
   `VTK_ROOT` 是安装目录，不是源码目录，不应使用 `VTK_ROOT/Examples` 查找源码。
2. 在对应分类下实现 `IExample`，标注 `[Example]`，沿用现有示例的运行与释放方式。
3. 若缺少 API，通过 [候选白名单流程](../../docs/generator.md#白名单变更流程) 补充，不直接手改生成文件。
4. 需要新增数据时记录来源及许可，并在项目中配置输出复制。
5. 运行相关构建、测试和目标示例；记录差异、未支持行为及人工验收结果。

优先选择尚未覆盖的 API 和最小可运行场景。自动截图能力通过可选的 `ISmokeExample` 提供，
当前首个实现是 Cone，不应假设每个示例都支持无交互验收。
