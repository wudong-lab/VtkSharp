# Legend 移植记录

- 官方源码：[Legend](https://examples.vtk.org/site/Cxx/Visualization/Legend/)，Attribution 2.5。
- 保留对象符号、名称与 RGB 颜色图例；使用程序生成的 Box、Ball 和主体球面，不依赖文件数据。
- `vtkLegendBoxActor.SetEntry` 接收源输出 PolyData、UTF-8 标签和 3 分量颜色；源数据在渲染结束前保持存活。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-200136-9b345a19/verification.json`；已人工检查截图中的图例文字与 Box/Ball 符号，仍需检查高 DPI。
