# BandedPolyDataContourFilter 移植记录

- 官方源码：[BandedPolyDataContourFilter](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/BandedPolyDataContourFilter/)，Attribution 2.5。
- 用规则平面上的 0–100 点标量生成 5 个等值分割值，展示过滤器生成的离散 CellData 色带；由 mapper 按单元标量着色。
- 原例使用不规则点集和两个面；此处使用规则平面以缩小数据构造范围，重点保持分带与单元着色行为。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-195658-e0b78996/verification.json`；仍需人工检查色带边界与平面覆盖。
