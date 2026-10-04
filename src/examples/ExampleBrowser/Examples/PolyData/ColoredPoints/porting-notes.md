# ColoredPoints 移植记录

- 官方源码：[ColoredPoints](https://examples.vtk.org/site/Cxx/PolyData/ColoredPoints/)，Attribution 2.5。
- 将三个无拓扑点经 `vtkVertexGlyphFilter` 转为可渲染顶点，并用 RGB unsigned-char 点标量着色；不依赖外部数据。
- 新增绑定：无。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-192707-b2b69c68/verification.json`；仍需人工检查截图、交互和重复创建/释放。
