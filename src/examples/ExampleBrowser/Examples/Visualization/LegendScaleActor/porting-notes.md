# LegendScaleActor 移植记录

- 官方源码：[LegendScaleActor](https://examples.vtk.org/site/Cxx/Annotation/LegendScaleActor/)，Attribution 2.5。
- 用球体提供可见的三维范围，比例尺演员根据场景坐标显示边轴与刻度；未附加工程单位。
- 复用已有 `vtkLegendScaleActor` 绑定；该示例展示空间尺度，不是数值云图色标。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-193836-daa27b87/verification.json`；屏幕截图与投影/单位语义仍需人工确认。
