# DijkstraGraphGeodesicPath 移植记录

- 官方源码：[DijkstraGraphGeodesicPath](https://examples.vtk.org/site/Cxx/PolyData/DijkstraGraphGeodesicPath/)，Attribution 2.5。
- 保留球面、顶点 0 到 7 的离散网格边最短路径与粉色叠加显示。
- 工程限制：Dijkstra 路径沿网格边行走，结果随网格拓扑和三角形方向变化，不是连续曲面测地线。
- 新增绑定：`vtkDijkstraGraphGeodesicPath`、`vtkGraphGeodesicPath.SetStartVertex/SetEndVertex` 及类型基类依赖。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-192233-65032e45/verification.json`；仍需人工检查截图、交互和重复创建/释放。
