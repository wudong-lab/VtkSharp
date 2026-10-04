# InterpolateTerrain 移植记录

- 官方源码：[InterpolateTerrain](https://examples.vtk.org/site/Cxx/PolyData/InterpolateTerrain/)，Attribution 2.5。
- 保留同一组 10×10 随机高程、栅格 `vtkProbeFilter` 查询和 TIN 垂直射线求交。为适配无渲染的原例，增加 TIN 与射线交点可视化，并将数值比较写入 `Debug` 输出；省略原例写入当前目录的 `surface.vtp` 副作用。
- 射线接口使用 `vtkCellLocator` 的交点集合重载，输入长度固定为 3 的 Span，输出为 `vtkPoints` 与 `vtkIdList`。
- 新增绑定：`vtkCellLocator`、基类 `vtkAbstractCellLocator`/`vtkLocator`、`vtkGenericCell`，以及必要的求交、数据集和输出访问函数；候选规划无 `needs-metadata`。
- 验证：待执行。
