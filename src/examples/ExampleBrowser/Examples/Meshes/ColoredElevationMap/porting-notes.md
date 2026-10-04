# ColoredElevationMap 移植记录

- 官方源码：[ColoredElevationMap](https://examples.vtk.org/site/Cxx/Meshes/ColoredElevationMap/)，Attribution 2.5。
- 保留带固定种子的扰动网格、Delaunay 三角化及按顶点 Z 值生成直接 RGB 颜色的流程。颜色在三角化前附加到输入点，避免依赖当前未封装的 `vtkPolyData.GetPoint`；VTK Delaunay 输出会传递点数据。无符号字节 RGB 数组由 mapper 默认按直接颜色处理。
- 为适配 ExampleBrowser，加入相机复位、固定窗口尺寸及 `ISmokeExample` 截图入口。无外部数据。
- 新增绑定：无。
- 验证：待执行。
