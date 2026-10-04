# 移植说明

- 原始示例：[TubesFromSplines.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/VisualizationAlgorithms/TubesFromSplines.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 保留样条中心线与可变半径管面；半径改为沿参数线性变化，以免暴露与示例无关的 tuple 指针接口。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
