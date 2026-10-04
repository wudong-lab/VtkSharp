# SimpleElevationFilter 移植记录

- 官方源码：[SimpleElevationFilter](https://examples.vtk.org/site/Cxx/Meshes/SimpleElevationFilter/)，Attribution 2.5。
- 保留规则点生成、Delaunay 三角化和 `vtkSimpleElevationFilter` 的向量投影 `(0, 0, 1)`；沿用官方整型除法生成高度。
- 官方手动构造 RGB 数组；此处让默认点标量直接经 LUT 映射，视觉语义相同且保留可解释的高程值。
- 使用最小 API 补充 `vtkSimpleElevationFilter.SetVector(double,double,double)`；候选和合并记录在 `artifacts/simple-elevation-*`。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-194822-75a0adaf/verification.json`；仍需人工检查视角和颜色分布。
