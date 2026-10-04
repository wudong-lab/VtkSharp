# 移植说明

- 原始示例：[TubesWithVaryingRadiusAndColors.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/VisualizationAlgorithms/TubesWithVaryingRadiusAndColors.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 半径由 `TubeRadius` 点数组控制，颜色由独立 `Colors` 点数组控制；螺旋参数与原例一致。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
