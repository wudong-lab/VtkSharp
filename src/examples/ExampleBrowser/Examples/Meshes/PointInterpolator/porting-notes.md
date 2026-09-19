# PointInterpolator 移植记录

- 官方页面与 C++ 源码：<https://examples.vtk.org/site/Cxx/Meshes/PointInterpolator/>
- 原始源码：`src/Cxx/Meshes/PointInterpolator.cxx`
- 移植目标：读取稀疏 `x/y/z/val` 点数据，将 `val` 使用 Gaussian kernel 插值到 STL 表面，
  同时显示插值表面和原始采样点。

## 数据文件

数据来自官方历史 VTKExamples 仓库 `lorensen/VTKExamples` 的 `master` revision
`a195d67ddd662c821431e07c5a14d1804af4153e`：

| 文件 | 来源地址 | SHA-512 |
| --- | --- | --- |
| `sparsePoints.txt` | <https://raw.githubusercontent.com/lorensen/VTKExamples/a195d67ddd662c821431e07c5a14d1804af4153e/src/Testing/Data/sparsePoints.txt> | `B6448C8668671DA5F6568AAF4B5FCB069AFAE735DF7000563138EB692E6D4C0E4C9705AEF5F96818F5F8E8545BD0BC0754CA2E1F8483748C6BE958782CC83807` |
| `InterpolatingOnSTL_final.stl` | <https://raw.githubusercontent.com/lorensen/VTKExamples/a195d67ddd662c821431e07c5a14d1804af4153e/src/Testing/Data/InterpolatingOnSTL_final.stl> | `7A72D4884D0F9E4446A9A05FB4793A684CEA8E9299083CDFEA1601D38A7A494901EF92BF17642FB580DF12D3F2B2ADE2E8F2ADEA93E6801F4616D6BDEA64962D` |

文件复制到 `Data/`，并由 `ExampleBrowser.csproj` 复制到应用输出目录的根 `Data/`。

## 翻译差异

- 官方示例通过命令行传入两个文件；ExampleBrowser 使用随示例部署的稳定 `Data/` 路径。
- C++ 的 `double* range = ...GetRange()` 改用 `Span<double>` 接收 `vtkDataArray.GetRange` 输出。
- `vtkNew<T>` 改为 `using var`；`vtkPolyData`、`vtkPointData` 和 `vtkDataArray` 的输出/属性包装器
  按 VTK 借用引用语义使用，不负责释放 native 对象。
- 增加 `ISmokeExample` 截图入口，正常运行仍进入交互循环。

## 新增绑定

- `vtkDelimitedTextReader`、`vtkTableToPolyData`、`vtkSTLReader`。
- `vtkGaussianKernel`、`vtkGeneralizedKernel`、`vtkInterpolationKernel`、`vtkPointInterpolator`。
- `vtkPointGaussianMapper`。
- `vtkDataSetAttributes.SetActiveScalars`、`vtkDataArray.GetRange(double[2])`。

候选与规划报告：`artifacts/point-interpolator/candidate.yml`、
`artifacts/point-interpolator/report.json`。

## 验证

- `plan-bindings`：通过，无歧义项和冲突。
- `diff-whitelist --summary`：新增 10 个类型、17 个函数；随后补充 `vtkDataArray.GetRange`。
- `generate-bindings --output-root src --incremental`：通过。
- `pwsh tools/verify-workflow.ps1 -VtkDir D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.7 -Regenerate -Example Meshes/PointInterpolator`：全部选定检查通过。
- 验证报告：`artifacts/verification/20260919-223152-8bc57613/verification.json`。
- Smoke 截图：`artifacts/verification/20260919-223152-8bc57613/example/screenshot.png`；已确认插值表面、采样点和颜色标量均正常显示。
- 尚未自动验证：长时间交互操作及重复创建/销毁窗口。
