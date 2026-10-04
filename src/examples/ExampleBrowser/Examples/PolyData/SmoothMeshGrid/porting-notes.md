# SmoothMeshGrid 移植记录

- 官方源码：[SmoothMeshGrid](https://examples.vtk.org/site/Python/PolyData/SmoothMeshGrid/)，Attribution 2.5。
- 将 NumPy 二维高度数组改为 C# 二维数组；保留 32×32 规则位置随机高程、手工三角网、CleanPolyData 共享边，以及 Loop/Butterfly 三次细分对比。
- 新增绑定：`vtkCleanPolyData`、`vtkLoopSubdivisionFilter`、`vtkButterflySubdivisionFilter` 及基类依赖，`vtkSubdivisionFilter.SetNumberOfSubdivisions`、`vtkProp3D.SetPosition(double,double,double)`。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-185205-55776653/verification.json`；仍需人工检查截图、交互和重复创建/释放。
