# 移植说明

- 原始示例：[LabeledMesh.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/Visualization/LabeledMesh.cxx)，仓库 revision：`3f2e3c4e4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 按业务要求，将原例自动生成的 VTK 索引替换为显式 `NodeId`（101、205、309、412）和 `ElementId`（7001、7004、7012）；节点与单元编号存入对应数据数组并作为标签显示。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查；验证报告为 `artifacts/verification/20261003-211047-65986a9b/verification.json`，截图为 `artifacts/verification/20261003-211047-65986a9b\examples-revised/Visualization_LabeledMesh/screenshot.png`。交互窗操作与重复创建/释放尚未人工检查。
