# ElevationFilter 移植记录

- 官方源码：[ElevationFilter](https://examples.vtk.org/site/Cxx/Meshes/ElevationFilter/)，Attribution 2.5。
- 保留 10×10 高程点、Delaunay 三角化和按指定 Z 方向生成 `Elevation` 标量。原 C++ 整数除法产生离散高程；C# 版本使用 double 除法展示连续值，`vtkElevationFilter` 输出被设置为 0–9。
- 原例额外创建 RGB 数组；这里让 mapper 直接通过同一范围的 lookup table 映射 `Elevation`，保留按高程着色语义并避免取数组值、逐点跨边界写 RGB。
- 新增绑定：`vtkElevationFilter` 与 `SetLowPoint`、`SetHighPoint`、`SetScalarRange` 的三 double / 双 double 重载。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-190549-e8cc054d/verification.json`；仍需人工检查截图、交互和重复创建/释放。
