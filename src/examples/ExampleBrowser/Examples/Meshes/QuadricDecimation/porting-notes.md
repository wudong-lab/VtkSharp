# QuadricDecimation 移植记录

- 官方示例：<https://github.com/Kitware/vtk-examples/tree/a834e7d5cbbb0db09853752b5fd32ba9940ca9ae/src/Cxx/Meshes/QuadricDecimation/>
- 固定源码 revision：a834e7d5cbbb0db09853752b5fd32ba9940ca9ae（Kitware/vtk-examples）。
- 移植差异：目标面数减少率设为 0.8，对球面表面做二次误差简化。
- 自动截图 smoke：16/16 示例全部通过。

- 绑定规划/合并报告：artifacts/fem-candidate-02g.yml、artifacts/fem-report-02g.json。Clip、Threshold 和二次四面体的数据输入 API 另由 artifacts/fem-candidate-04.yml、artifacts/fem-report-04.json 补充。
- 构建验证：统一工作流通过；截图 smoke 16/16 通过。


- 最终统一验证报告：artifacts/verification/20261003-223204-62ef6815/verification.json。生成器测试、绑定生成、native 构建、managed 测试、ExampleBrowser 构建、Hexahedron smoke 和生成一致性检查均通过。
- 16 个新增示例的截图 smoke 均通过；PNG 与 result.json 保存在 artifacts/fem-smoke-final6/。交互行为和重复创建/销毁仍需人工确认。
