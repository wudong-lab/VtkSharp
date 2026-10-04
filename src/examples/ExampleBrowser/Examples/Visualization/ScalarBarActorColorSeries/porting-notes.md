# ScalarBarActorColorSeries 移植记录

- 官方源码：[ScalarBarActorColorSeries](https://examples.vtk.org/site/Cxx/Visualization/ScalarBarActorColorSeries/)，Attribution 2.5。
- 用 ColorBrewer 颜色系列构建离散 LUT，球面高程标量和 ScalarBarActor 共享 LUT 与 0–1 映射范围。
- 绑定未暴露 `vtkColorSeries::ORDINAL` 枚举常量；按当前 VTK 头文件中的枚举值传入 `0`，颜色系列按序号映射。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-193653-397b0041/verification.json`；仍需人工检查颜色呈现和重复创建/释放。
