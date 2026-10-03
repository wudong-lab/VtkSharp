# 移植说明

- 原始示例：[WarpVector.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/PolyData/WarpVector.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 按业务要求以 16 段采样每个梁单元；轴向平移线性插值，两个局部横向位移使用 Euler–Bernoulli 三次 Hermite 形函数并结合端点转角。转角输入约定为全局弧度，弯曲平面斜率按右手局部坐标投影。最终位移数组由 `vtkWarpVector` 按比例放大显示。
- 示例数据是手工构造的两个非零长度单元，无外部数据文件。此插值面向小变形线弹性梁，未处理剪切变形与几何非线性。
- 验证结果待统一验证流程完成后补记。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查；验证报告为 `artifacts/verification/20261003-211047-65986a9b/verification.json`，截图为 `artifacts/verification/20261003-211047-65986a9b\examples-revised/PolyData_WarpVector/screenshot.png`。交互窗操作与重复创建/释放尚未人工检查。
