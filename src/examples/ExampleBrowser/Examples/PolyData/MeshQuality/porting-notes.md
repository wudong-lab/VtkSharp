# MeshQuality 移植记录

- 官方示例：<https://github.com/Kitware/vtk-examples/tree/a834e7d5cbbb0db09853752b5fd32ba9940ca9ae/src/Cxx/PolyData/MeshQuality/>
- 固定源码 revision：a834e7d5cbbb0db09853752b5fd32ba9940ca9ae（Kitware/vtk-examples）。
- 移植差异：对球面三角网格计算三角形面积，并按单元标量着色；移除逐项控制台列表。
- 自动截图 smoke：16/16 示例全部通过。

- Clip、Threshold 和二次四面体需额外的数据输入 API。
- 构建验证：统一工作流通过；截图 smoke 16/16 通过。


- 生成器测试、绑定生成、native 构建、managed 测试、ExampleBrowser 构建、Hexahedron smoke 和生成一致性检查均通过。
- 16 个新增示例的截图 smoke 均通过；交互行为和重复创建/销毁仍需人工确认。
