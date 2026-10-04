# TriangulateTerrainMap 移植记录

- 官方源码：[TriangulateTerrainMap](https://examples.vtk.org/site/Cxx/Filtering/TriangulateTerrainMap/)，Attribution 2.5。
- 生成 10×10 XY 网格并用 VTK 的最小标准随机序列生成高程；保留点云与 Delaunay 三角网叠加显示。
- 为适配 ExampleBrowser，加入相机复位、固定窗口尺寸及 `ISmokeExample` 截图入口。无外部数据。
- 新增绑定：无。
- 验证：待执行。
