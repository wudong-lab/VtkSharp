# LineOnMesh 移植记录

- 官方源码：[LineOnMesh](https://examples.vtk.org/site/Python/DataManipulation/LineOnMesh/)，Attribution 2.5。
- 保留规则随机地形、Loop 细分、垂直射线采样及参数样条。样点从原例 1000 降至 100，降低托管到 native 的定位调用量；`vtkCellLocator.IntersectWithLine` 采用输出点集重载，避免 Python mutable 输出参数。
- 示例需要沿地形表面生成路线；样条会在离散采样点之间偏离曲面，不能视作严格贴面路径。
- 新增绑定：无（复用 SmoothMeshGrid 和 InterpolateTerrain 补充的 API）。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-185537-0c4a37e7/verification.json`；仍需人工检查截图、交互和重复创建/释放。
