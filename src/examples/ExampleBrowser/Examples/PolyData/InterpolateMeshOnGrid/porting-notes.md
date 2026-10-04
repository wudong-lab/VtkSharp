# InterpolateMeshOnGrid 移植记录

- 官方源码：[InterpolateMeshOnGrid](https://examples.vtk.org/site/Cxx/PolyData/InterpolateMeshOnGrid/)，Attribution 2.5。
- 保留散点高程、Delaunay、Probe、Warp 和重采样结果三角化流程。将未封装的 `SetSourceConnection` 替换为 `SetSourceData`：先更新源 Delaunay，再把其输出数据设为 Probe 源；数据结果等价，保留管线结果但不再通过连接自动传播上游更新。
- 原例写入三个 VTP 文件；ExampleBrowser 版本省略这些工作目录副作用，仅展示原表面和重采样表面。
- 新增绑定：无；绑定规划确认 `SetSourceData`、标量数组写入和交互样式所需 API 已存在。
- 验证：待执行。
