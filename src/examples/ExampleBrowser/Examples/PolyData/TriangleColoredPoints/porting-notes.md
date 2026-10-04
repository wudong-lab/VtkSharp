# TriangleColoredPoints 移植记录

- 官方源码：[TriangleColoredPoints](https://examples.vtk.org/site/Cxx/PolyData/TriangleColoredPoints/)，Attribution 2.5。
- 将 RGB unsigned-char 数组附加到三角形点数据，展示顶点颜色在面内的插值；不依赖外部数据。
- 新增绑定：无。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-192606-816550a2/verification.json`；仍需人工检查截图、交互和重复创建/释放。
