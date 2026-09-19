# Triangulate 移植记录

- 官方源码：<https://examples.vtk.org/site/Cxx/Meshes/Triangulate/>
- 原始文件：`src/Cxx/Meshes/Triangulate.cxx`
- 移植目标：并排显示 `vtkRegularPolygonSource` 输出的原始多边形和经 `vtkTriangleFilter` 三角化后的网格。

## 翻译差异

- `vtkNew<T>` 转为 C# `using var`，确保示例结束时释放 wrapper。
- 原始 C++ 的四元素 viewport 数组使用 `SetViewport(double, double, double, double)` 重载。
- `vtkNamedColors::GetColor3d(...).GetData()` 使用 VtkSharp 的 `VtkColor3d` 重载。
- 为 ExampleBrowser 增加 `ISmokeExample` 截图路径；正常 `Run()` 仍进入交互循环。

## 绑定与验证

- 候选与规划报告：`artifacts/triangulate/candidate.yml`、`artifacts/triangulate/report.json`。
- 新增绑定：`vtkTriangleFilter`；同时合并 `vtkProperty.Representation` 的枚举契约。
- 统一验证报告待执行 `tools/verify-workflow.ps1 -VtkDir <vtk-cmake-directory> -Regenerate -Example Meshes/Triangulate` 后补充。

## 未自动验证项

- 交互旋转、缩放和重复创建/销毁需要人工确认。
