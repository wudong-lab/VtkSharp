# ScalarBarActor 移植记录

- 官方源码：[ScalarBarActor](https://examples.vtk.org/site/Cxx/Visualization/ScalarBarActor/)，Attribution 2.5。
- 使用同一个 lookup table 驱动球面 mapper 与数值色标，范围均为 0–1；不依赖外部数据。
- 复用已有 `vtkElevationFilter`、`vtkLookupTable` 和 `vtkScalarBarActor` 绑定。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-193410-1c6db691/verification.json`；仍需人工检查截图、交互和重复创建/释放。
