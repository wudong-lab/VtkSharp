---
name: port-vtk-example
description: Translate or port a VTK C++ example from a URL, local source file, or example name into the VtkSharp C# ExampleBrowser, supplementing only the required VTK bindings through the candidate whitelist workflow. Use for VTK example translation, example migration, missing wrapper discovery, and example-specific binding additions. Do not use for unrelated VtkSharp API additions without an example.
---

# 翻译 VTK 示例

在仓库根目录工作。修改前阅读 `references/workflow.md`，了解源码获取、翻译和验证约定。

- 读取实际 C++ 源码，沿用当前 ExampleBrowser 结构，只添加示例需要的 API。
- 使用 `plan-bindings` 批量定位声明类、比对现有绑定并生成候选；仅需类型时使用 `classOnly`，不要为了添加类型而引入无关方法。
- 先读规划摘要，按类或状态展开待处理诊断；审核 `diff-whitelist --summary` 的全部新增项、依赖和冲突，再通过 `merge-candidate` 修改正式白名单。单类旧入口 `create-candidate` 必须使用 `--supported-only`。
- `ambiguous` 用签名 ID 明确选择；`needs-metadata` 的方向、长度和所有权需检查原生契约，不能猜测。
- 回调使用 managed `AddObserver`，不直接暴露 `vtkCallbackCommand`。生成代码的问题修复在 generator/配置中，不手改生成文件。
- 示例依赖外部数据时，先按“数据文件获取”流程解析并准备输入文件，再翻译和验证示例。
- 完成前用统一验证脚本检查构建、测试和生成一致性；目标示例未适配自动验收时手动运行，不能用其他示例通过代替。人工确认的互操作契约保留源码依据。

Windows 使用 PowerShell。候选、需求与报告放在仓库外或忽略的输出目录。

## 数据文件获取

官方示例页面不会总是给出可直接点击的数据文件。遇到 `vtkXMLPolyDataReader`、
`vtkImageReader`、`vtkPLYReader` 等外部输入时，按以下顺序定位来源：

1. 阅读官方实际 C++ 源码、`CMakeLists.txt` 和页面 Description，记录所有文件名、相对目录、
   命令行参数和环境变量。不要根据 reader 类型或文件扩展名猜数据集；例如
   `DeformPointSet` 无参数时自己生成球面，只有传入 `.vtp` 路径时才需要外部文件。
2. 优先查 `Kitware/vtk-examples` 的 `src/Testing/Data`。单文件使用仓库固定 revision 的
   raw URL 下载；目录数据必须保留 `src/Testing/Data/<relative-path>` 的相对结构。若示例依赖
   多个文件或整个子目录，优先获取对应目录，必要时获取完整仓库，避免只下载入口文件导致
   `.raw`、纹理、序列帧或附属文件缺失。
3. 若本机存在 VTK 源码树，优先检查 `<vtk-source>/Testing/Data`。VTK 新版通常使用
   CMake `ExternalData` 管理大文件：源码目录里的 `file.sha512` 只是哈希描述文件，不是真实
   数据文件；只有找到同名实体文件，或找到已配置的数据对象缓存
   `<vtk-build>/ExternalData/Objects`、`<vtk-source>/.ExternalData`、外部 `VTKData` 目录时，
   才能直接复制。不要把 `.sha512` 文件改名成数据文件，也不要把未展开的占位文件当作验证数据。
   如果只有哈希描述文件，应让 VTK 的 ExternalData/CMake 流程先下载并展开，或根据哈希和
   VTK 官方 ExternalData URL 模板获取文件；记录源码 revision、SHA-512 和实际文件来源。
4. 若文件不在 vtk-examples 数据目录，再查官方 VTK 数据包对应版本的
   `Testing/Data`，以及 VTK 构建目录的 `ExternalData`。记录 VTK/示例仓库 revision；不要把
   PyVista 等第三方镜像当作首选来源，只有官方源不可用时才可作为明确标注的备用源。
5. 下载到示例目录下的 `Data/`（或当前 ExampleBrowser 约定的数据目录），使用
   `Invoke-WebRequest -Uri <raw-url> -OutFile <path>`。下载前创建父目录；下载后检查文件存在、
   文件大小非零，并对文本/归档响应误存为数据文件保持警惕。重复移植时先复用已有文件。
6. 对每个实际使用的数据文件，必须在 `porting-notes.md` 逐文件记录来源地址：原始下载 URL
   或官方仓库/源码路径、仓库 revision 或版本、SHA-512（若使用 ExternalData）、目标相对路径、
   文件名和是否包含附属文件。即使文件来自本地 VTK 源码树，也要记录本地相对路径及其对应的
   官方仓库地址；不能只写“来自本地 VTK”。许可证或第三方数据授权不明确时不要自动提交数据
   文件，改为记录获取说明并请求确认。
7. C# 示例不能依赖用户当前目录、机器上的 `VTK_DATA_ROOT` 或命令行参数，除非
   ExampleBrowser 已明确提供该机制。默认将数据作为示例资源复制到输出目录，并在代码中使用
   稳定的示例资源路径；若数据过大或授权限制不适合提交，保留下载脚本/说明，并在运行前给出
   清晰的缺失数据提示。

推荐的 PowerShell 模板（将 URL 和路径替换为已从官方源码确认的值）：

```powershell
$dataDirectory = "<example-directory>\Data"
$dataPath = Join-Path $dataDirectory "<relative-file>"
New-Item -ItemType Directory -Force (Split-Path $dataPath) | Out-Null
if (-not (Test-Path $dataPath)) {
    Invoke-WebRequest -Uri "<pinned-raw-url>" -OutFile $dataPath
}
if ((Get-Item $dataPath).Length -eq 0) {
    throw "Downloaded data file is empty: $dataPath"
}
```

数据获取不是绑定候选的替代步骤：先准备数据，再从源码编译示例并按统一验证流程确认 reader
确实能打开该文件；无法联网时应报告精确 URL 和目标路径，不要用未验证的占位文件冒充验证通过。
