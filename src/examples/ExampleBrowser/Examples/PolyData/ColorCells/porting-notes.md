# ColorCells 移植记录

- 官方源码：[ColorCells](https://examples.vtk.org/site/Cxx/PolyData/ColorCells/)，Attribution 2.5。
- 为四边形网格单元附加整数标量，经 lookup table 映射颜色；CellData 元组数与单元数一致，不依赖外部数据。
- 新增绑定：无。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-193023-57d5701a/verification.json`；仍需人工检查截图、交互和重复创建/释放。
