# ColoredSphere 移植记录

- 官方源码：[ColoredSphere](https://examples.vtk.org/site/Cxx/Rendering/ColoredSphere/)，Attribution 2.5。
- 使用 `vtkElevationFilter` 沿 Z 方向生成 0–1 标量并由 lookup table 映射球面颜色；不依赖外部数据。
- 复用前序绑定：`vtkElevationFilter` 与 `SetLowPoint`、`SetHighPoint`、`SetScalarRange`。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-192924-e4672ad3/verification.json`；仍需人工检查截图、交互和重复创建/释放。
