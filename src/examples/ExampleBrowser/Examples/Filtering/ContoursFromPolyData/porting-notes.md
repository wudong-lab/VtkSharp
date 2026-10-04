# ContoursFromPolyData 移植记录

- 官方源码：[ContoursFromPolyData](https://examples.vtk.org/site/Cxx/Filtering/ContoursFromPolyData/)，Attribution 2.5。
- 保留官方无文件参数时创建球面、中心平面和多个平行截面；将边界盒对角线距离预先固定为球半径对应值 1.73，输入不依赖命令行或外部 VTP 文件。
- 新增绑定：无；复用 `Cutter` 示例的 cutter / plane API。
- 新增绑定：`vtkCutter.GenerateValues(int,double,double)`。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-191450-c574a3e5/verification.json`；仍需人工检查截图、交互和重复创建/释放。
