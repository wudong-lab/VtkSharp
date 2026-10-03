# 移植说明

- 原始示例：[OrientedCylinder.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/GeometricObjects/OrientedCylinder.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 用托管 double 向量构造圆柱局部 Y 轴的右手基，并用 actor user matrix 同时旋转、缩放到两点间；半径固定为 0.12。
- 使用固定非退化端点，未移植原例的随机数与起终点球体。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查；验证报告为 `artifacts/verification/20261003-211047-65986a9b/verification.json`，截图为 `artifacts/verification/20261003-211047-65986a9b\examples-revised/GeometricObjects_OrientedCylinder/screenshot.png`。交互窗操作与重复创建/释放尚未人工检查。
