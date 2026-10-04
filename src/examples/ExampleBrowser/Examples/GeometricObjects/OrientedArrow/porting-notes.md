# 移植说明

- 原始示例：[OrientedArrow.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/GeometricObjects/OrientedArrow.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 用托管 double 向量计算右手局部基，并写入 actor user matrix；箭头局部 X 轴从起点映射到终点，避免额外导出静态 `vtkMath` API。
- 使用固定非退化端点，未移植原例的随机数与起终点球体。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
