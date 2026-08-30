# 构建 VtkSharp

VtkSharp 由 managed wrapper 和聚合了静态 VTK 的 `VtkSharp.Native.dll` 组成。两部分必须使用匹配的配置构建。

## CRT 与配置匹配

native DLL、VTK 静态库和消费它的 managed 构建必须使用一致配置：

| Managed 配置 | Native 配置 | MSVC CRT |
| --- | --- | --- |
| `Debug` | `Debug` | `/MDd` |
| `Release` | `Release` | `/MD` |

不能把 Debug wrapper、Release native DLL 或不同配置的 VTK 静态库混合部署。`VtkSharp.csproj` 会根据 `$(Configuration)` 选择相应 native 产物。

## 分步构建

先按 [VTK 构建说明](vtk.md) 安装对应配置的 VTK，再执行：

```powershell
$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.7"

.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir
dotnet build src/bindings/VtkSharp.slnx --configuration Release
```

Debug 构建使用相同命令并将配置改为 `Debug`。

## 一键构建与产物

```powershell
.\tools\build-all.ps1 -Configuration Release -VtkDir $vtkDir
```

脚本构建 managed/native 项目，成功后将不同 TFM 的发布文件收集到 `artifacts/bin`，包括与 `VtkSharp.dll` 同目录的 XML API 文档 `VtkSharp.xml`。每个可部署目录均应包含与其配置匹配的 `VtkSharp.Native.dll`。

如已存在有效的 native 构建，可按脚本参数跳过 native 重建；跳过前必须确认 DLL 的 VTK 版本、工具集、平台和配置一致。

## 提交前验证

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir
dotnet test src/bindings/VtkSharp.slnx --configuration Release
dotnet build src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release
```

涉及渲染或交互的改动还应运行对应示例，确认首帧、交互和释放流程正常。
