# 桥梁杆系与结构实体渲染示例移植清单

## 节点、梁杆单元与编号

- [x] [ColoredLines](https://examples.vtk.org/site/Cxx/GeometricObjects/ColoredLines/)：杆系连接关系与单元独立着色；难度低到中；第一批移植。
- [x] [Glyph3DMapper](https://examples.vtk.org/site/Cxx/Visualization/Glyph3DMapper/)：批量节点符号及颜色、大小控制；难度中；第一批移植。
- [x] [TubeFilter](https://examples.vtk.org/site/Cxx/PolyData/TubeFilter/)：中心线生成圆管状表面；难度低；第一批移植。
- [x] [LabeledMesh](https://examples.vtk.org/site/Cxx/Visualization/LabeledMesh/)：节点与单元编号显示；难度中；第一批移植，标签绑定业务 `NodeId`、`ElementId`。
- [x] [TubesWithVaryingRadiusAndColors](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/TubesWithVaryingRadiusAndColors/)：半径与颜色分别按数组控制；难度低到中。
- [x] [OrientedArrow](https://examples.vtk.org/site/Cxx/GeometricObjects/OrientedArrow/)：集中荷载、反力与单元局部坐标轴符号；难度低到中。

## 结构几何实体

- [x] [OrientedCylinder](https://examples.vtk.org/site/Cxx/GeometricObjects/OrientedCylinder/)：任意两点间圆柱定位；难度低到中；可用于圆杆、索与圆柱墩。
- [x] [LinearExtrusion](https://examples.vtk.org/site/Cxx/PolyData/LinearExtrusion/)：截面沿梁轴拉伸；原例难度低到中，业务扩展难度中；第一批移植，原例文字截面需替换为业务截面，并采用单元局部坐标系定位。
- [x] [TubesFromSplines](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/TubesFromSplines/)：曲线管状表面；难度中；可用于索和曲线管件的几何显示。
- [x] [ExtrudePolyDataAlongLine](https://examples.vtk.org/site/Cxx/Visualization/ExtrudePolyDataAlongLine/)：沿空间曲线布置截面并连接表面；难度高；需核查额外模块依赖、表面完整性与截面方向。

## 杆系结果显示

- [x] [WarpVector](https://examples.vtk.org/site/Cxx/PolyData/WarpVector/)：节点位移与变形后杆系显示；难度低到中；第一批移植，梁弯曲变形需按单元插值关系结合节点平移和转角生成采样点。
- [x] [RibbonFilter](https://examples.vtk.org/site/Cxx/PolyData/RibbonFilter/)：线生成带状表面；难度低到中；可参考结果图几何生成，弯矩、剪力及局部轴语义由业务层定义。
