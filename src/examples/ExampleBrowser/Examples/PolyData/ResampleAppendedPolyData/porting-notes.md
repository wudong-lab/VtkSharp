# ResampleAppendedPolyData 移植记录

- 官方源码：[ResampleAppendedPolyData](https://examples.vtk.org/site/Cxx/PolyData/ResampleAppendedPolyData/)，Attribution 2.5。
- 保留随机 Platonic solids、平面地形追加、CellLocator 垂直求交和左右视口对比。使用已验证的交点集合重载替代原例输出参数数组。
- 为 ExampleBrowser 将 probe 分辨率从 200 降至 50，避免 40,000 次 managed/native 调用拖慢示例启动；地形和物体数量沿用原值。探测网格改高程后保留平面法线，但右侧以 wireframe 显示，不依赖该法线着色。
- 新增绑定：`vtkPlatonicSolidSource`、`vtkTransformFilter`，以及平面分辨率、solid type 选择、`vtkPolyData.DeepCopy`、`vtkTransformFilter.SetTransform` 等调用；候选规划无未解决输出元数据项。SolidType enum 映射只新增于新包装类型。
- 验证：待执行。
