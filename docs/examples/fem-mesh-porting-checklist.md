# 通用有限元与网格处理示例移植清单

## 网格构造、读取与渲染

- [x] [Hexahedron](https://examples.vtk.org/site/Cxx/GeometricObjects/Hexahedron/)：八节点六面体构造与显示；难度中；优先移植。
- [x] [ReadUnstructuredGrid](https://examples.vtk.org/site/Cxx/IO/ReadUnstructuredGrid/)：读取 `.vtu` 非结构网格；难度中；优先移植。
- [x] [DataSetSurfaceFilter](https://examples.vtk.org/site/Cxx/PolyData/DataSetSurfaceFilter/)：提取体网格外表面；难度中；优先移植，原例需补渲染。
- [x] [QuadraticTetra](https://examples.vtk.org/site/Cxx/GeometricObjects/QuadraticTetra/)：二次四面体与细分显示；难度中高。

## 网格质量与处理算法

- [x] [MeshQuality](https://examples.vtk.org/site/Cxx/PolyData/MeshQuality/)：单元指标计算与着色；难度中；优先移植，原例使用三角形面积指标。
- [x] [HighlightBadCells](https://examples.vtk.org/site/Cxx/PolyData/HighlightBadCells/)：按指标筛选并高亮单元；难度中；原例面积阈值需按业务调整。
- [x] [ConstrainedDelaunay2D](https://examples.vtk.org/site/Cxx/Filtering/ConstrainedDelaunay2D/)：带边界约束的二维三角剖分；难度低到中。
- [x] [Delaunay3D](https://examples.vtk.org/site/Cxx/Modelling/Delaunay3D/)：点集四面体化与 Alpha 参数；难度中高；复杂 CAD 域工程网格生成需另行评估。
- [x] [ConnectivityFilter](https://examples.vtk.org/site/Cxx/Filtering/ConnectivityFilter/)：连通区域分组、着色与提取；难度中。
- [x] [WindowedSincPolyDataFilter](https://examples.vtk.org/site/Cxx/Meshes/WindowedSincPolyDataFilter/)：表面平滑；难度低到中。
- [x] [QuadricDecimation](https://examples.vtk.org/site/Cxx/Meshes/QuadricDecimation/)：表面三角网格简化；难度低到中。

## 后处理与选择

- [x] [Cutter](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/Cutter/)：平面截面；难度低到中；优先移植，有限元应用需采用体网格输入。
- [x] [ClipUnstructuredGridWithPlane](https://examples.vtk.org/site/Cxx/UnstructuredGrid/ClipUnstructuredGridWithPlane/)：体网格平面裁剪；难度中；保留求解器原始编号映射。
- [x] [ThresholdCells](https://examples.vtk.org/site/Cxx/PolyData/ThresholdCells/)：按单元属性筛选；难度中；原例需补渲染。
- [x] [ExtractSelectionOriginalId](https://examples.vtk.org/site/Cxx/PolyData/ExtractSelectionOriginalId/)：提取前后原始点 ID 映射；难度中；可参考扩展单元 ID 映射。
- [x] [CellPicking](https://examples.vtk.org/site/Cxx/Picking/CellPicking/)：单元拾取与高亮；难度高；需适配托管事件与选择管线。

## 已有移植，可直接复用

- [x] [BoundaryEdges](https://examples.vtk.org/site/Cxx/Meshes/BoundaryEdges/)：表面开放边界提取与显示。
- [x] [Triangulate](https://examples.vtk.org/site/Cxx/Meshes/Triangulate/)：多边形表面三角化。
- [x] [DelaunayMesh](https://examples.vtk.org/site/Cxx/Modelling/DelaunayMesh/)：二维点集三角剖分。
- [x] [LabelContours](https://examples.vtk.org/site/Cxx/Visualization/LabelContours/)：等值线及数值标注。
- [x] [PointInterpolator](https://examples.vtk.org/site/Cxx/Meshes/PointInterpolator/)：稀疏采样值向表面映射；有限元网格结果采样应另外评估 `vtkProbeFilter`。
