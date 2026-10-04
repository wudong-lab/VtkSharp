# DEMReader 移植记录

- 官方源码：[DEMReader](https://examples.vtk.org/site/Cxx/IO/DEMReader/)，Attribution 2.5。
- 读取仓库已有的 `FitToHeightMap/Data/SainteHelens.dem`，使用同一输出资源路径 `Data/SainteHelens.dem`；数据来源和 SHA-512 记录见 [FitToHeightMap 移植记录](../../Meshes/FitToHeightMap/porting-notes.md)。
- 保留 DEM 读取、色表映射和图像交互；ExampleBrowser 模式使用稳定资源路径并增加截图入口。
- 新增绑定：`vtkImageSlice.GetMapper()`，供 `vtkImageActor` 设置输入连接；其 mapper 由 actor 持有，C# 返回包装不转移 native 所有权。
- 验证：待执行。
