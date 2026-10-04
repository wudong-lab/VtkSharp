# CellPicking 移植记录

- 官方示例：<https://github.com/Kitware/vtk-examples/tree/a834e7d5cbbb0db09853752b5fd32ba9940ca9ae/src/Cxx/Picking/CellPicking/>
- 固定源码 revision：a834e7d5cbbb0db09853752b5fd32ba9940ca9ae（Kitware/vtk-examples）。
- 移植差异：将自定义 C++ 交互器样式替换为托管 LeftButtonPressEvent observer；picker/renderer 通过 clientData 保活，命中 cell ID 与世界坐标写入 Debug 输出。
- 自动截图 smoke：16/16 示例全部通过。

- Clip、Threshold 和二次四面体需额外的数据输入 API。
- 构建验证：统一工作流通过；截图 smoke 16/16 通过。


- 生成器测试、绑定生成、native 构建、managed 测试、ExampleBrowser 构建、Hexahedron smoke 和生成一致性检查均通过。
- 16 个新增示例的截图 smoke 均通过；交互行为和重复创建/销毁仍需人工确认。
