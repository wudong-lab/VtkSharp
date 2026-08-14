# FitToHeightMap 移植记录

- 官方源码：<https://examples.vtk.org/site/Cxx/Meshes/FitToHeightMap/>
- 数据来源：VTK Examples 的 `src/Testing/Data/SainteHelens.dem`
- 移植目标：保持原示例的三个视口、共享相机和两种地形贴合策略。

## 使用的 VTK 类型

`vtkDEMReader`、`vtkImageDataGeometryFilter`、`vtkWarpScalar`、`vtkProbeFilter`、
`vtkFitToHeightMapFilter`、`vtkPlaneSource`、`vtkLookupTable`、`vtkPolyDataMapper`、
`vtkActor`、`vtkRenderer`、`vtkRenderWindow` 和 `vtkRenderWindowInteractor`。

## 新增绑定

- 新类型：`vtkDEMReader`、`vtkImageDataGeometryFilter`、`vtkWarpScalar`、
  `vtkProbeFilter`、`vtkFitToHeightMapFilter`。
- 既有类型补充：`vtkDataSet.GetBounds/GetScalarRange`、
  `vtkMapper.SetLookupTable/ScalarVisibilityOn/SetScalarRange`、
  `vtkPlaneSource.SetPoint1/SetPoint2/SetResolution`、`vtkViewport.SetViewport`。
- 新增模块：`vtkFiltersGeometry`。生成器会将其加入 native 查找、链接和
  `vtk_module_autoinit`。

## 与官方源码的差异

- ExampleBrowser 不接受命令行数据路径，因此将官方 DEM 数据复制到输出目录的
  `Data/SainteHelens.dem`。
- 显式调用 `vtkImageDataGeometryFilter.SetOutputTriangles(false)`，保持其默认四边形输出，
  同时使该具体类型包含一个由示例实际调用的直接声明方法。
- 使用 `Span<double>` 接收 bounds 和 scalar range，避免暴露 native 指针。

## 验证

- `validate-whitelist` 通过。
- `generate-bindings --check` 通过。
- Release native 构建通过，`vtkFiltersGeometry` 已参与链接和 autoinit。
- `VtkSharp.Tests` 34 项测试通过，ExampleBrowser Release 构建通过。
- 已实际运行示例，三个视口均正确显示圣海伦火山地形；相机拖动和窗口关闭正常。
