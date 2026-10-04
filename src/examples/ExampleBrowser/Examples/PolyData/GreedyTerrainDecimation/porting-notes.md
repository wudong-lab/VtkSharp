# GreedyTerrainDecimation 移植记录

- 官方源码：[GreedyTerrainDecimation](https://examples.vtk.org/site/Cxx/PolyData/GreedyTerrainDecimation/)，Attribution 2.5。
- 按原例以固定种子生成 3×3 字节高程栅格，并用 `vtkGreedyTerrainDecimation` 构建带边线的地形网格。将原例的 `GetScalarPointer` 裸指针写入替换为 `SetScalarComponentFromDouble`，保持像素类型和数值范围一致。
- 新增绑定：`vtkGreedyTerrainDecimation` 类型（FiltersHybrid）。
- 局限：3×3 原始数据仅演示管线，不足以评估真实 DEM 的简化效果或高程误差。
- 验证：待执行。
