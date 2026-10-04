# ScalarBarWidget 移植记录

- 官方源码：[ScalarBarWidget](https://examples.vtk.org/site/Cxx/Widgets/ScalarBarWidget/)，Attribution 2.5。
- 原例依赖的 `uGridEx.vtk` 和 `vtkUnstructuredGridReader` 未用于此最小移植；改为程序生成球面和高程标量，以保留可交互色标的核心行为。
- Widget 管理的 ScalarBarActor 与球面 Mapper 共用同一个 LUT，数值范围均为 0–1。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-193940-f50fd467/verification.json`；截图烟测未触发鼠标事件，需人工验证拖动、缩放、窗口尺寸变化和关闭释放。
