# 移植说明

- 原始示例：[ExtrudePolyDataAlongLine.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/Visualization/ExtrudePolyDataAlongLine.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 官方原例无文件参数时会从 `vtkDiskSource` 程序化生成三点截面；本移植使用圆形截面沿解析曲线逐站扫掠，利用投影运输截面法向减少 Frenet frame 在低曲率段的翻转，并闭合两端形成完整表面。
- 网格含 81 个截面、每圈 16 点，侧壁为四边形，端盖为多边形；没有读取或提交外部数据。
- VTK 原例使用 `vtkFrenetSerretFrame`，当前安装的 VTK hierarchy 不含该类型，因此托管侧计算中心线和截面局部系。
- 验证结果待统一验证流程完成后补记。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查；验证报告为 `artifacts/verification/20261003-211047-65986a9b/verification.json`，截图为 `artifacts/verification/20261003-211047-65986a9b/Visualization_ExtrudePolyDataAlongLine/screenshot.png`。交互窗操作与重复创建/释放尚未人工检查。
