# 构建与打包 VtkSharp

当前默认将 VTK 9.7.0 静态链接到单一 `VtkSharp.Native.dll`，managed wrapper 通过 P/Invoke 调用。保留 Windows x64、Release `/MD` 和 Debug `/MDd`；部署不需要 VTK 模块 DLL，目标机器仍需要兼容的 x64 Visual C++ 运行库。

## 构建

`VTK_DIR` 必须指向静态 VTK 的 CMake package；不能继续使用此前的 shared 安装。旧静态安装可以复用，但需要包含当前白名单使用的全部模块和第三方许可文件。

```powershell
$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.7"
.\tools\build-all.ps1 -Configuration Release -VtkDir $vtkDir
```

构建与打包默认 `-Linkage Static`，汇总产物位于 `artifacts/bin/<Configuration>/<TFM>`。仅执行 `dotnet build` 不会编译 native 层。公开构建生成仅包含公开导出的 DLL；私有产品通过 `VTKSHARP_EXTRA_NATIVE_SOURCES` 等聚合接口，把公开和私有 native 源码共同链接为一个同名 DLL。

## 本地 NuGet 包

```powershell
.\tools\package-nuget.ps1 -Configuration Release -Version 26.1007.5 -VtkDir $vtkDir
```

打包只接受 Release，生成本地包而不发布；同版本产物不覆盖。静态包只携带 `VtkSharp.Native.dll` 和 VTK／第三方许可声明。包内 `buildTransitive/VtkSharp.targets` 将 DLL 复制到 build 和 publish 输出目录，与 `VtkSharp.dll` 同级。

P/Invoke 按 .NET/Windows 常规规则从应用目录解析 `VtkSharp.Native.dll`，不提供自定义运行目录配置。私有产品必须排除公开 NuGet 的 native 和构建复制资产，部署包含两侧导出的统一 DLL；禁止同时部署两份分别静态链接 VTK 的公开、私有入口。公开源码修订与托管包必须配套。

保留显式 `-Linkage Dynamic` 作为可选构建路径，使用独立缓存及 shared VTK；它同样生成单一公开入口，不再按模块生成多个 DLL。
