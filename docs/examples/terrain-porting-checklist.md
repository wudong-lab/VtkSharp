# 地形建模与处理示例移植清单

本文整理 VTK 官方示例中与地形建模、地形处理相关的示例，并基于当前 VtkSharp 绑定和 ExampleBrowser 评估移植难度。

- 官方入口：[C++ 示例目录](https://examples.vtk.org/site/Cxx/)、[Python 示例目录](https://examples.vtk.org/site/Python/)。相同示例的多语言版本不重复列入清单。
- 移植目标：当前 C# ExampleBrowser，包括必要的绑定补充、数据准备和验证。
- 初始清单日期：2026-10-03；本轮移植记录：2026-10-04。
- 本轮对新增示例运行了统一快速验证，并对所需 API 使用候选白名单流程；各项结果、实现差异和仍需人工确认的内容见对应示例 `porting-notes.md`。
- 本文是筛选和实施参考，不是完整 GIS 功能清单；不能据此推断坐标基准或真实 DEM 精度已经验收。

## 难度与状态说明

| 标记 | 含义 |
| --- | --- |
| 低 | 主要是 C# 翻译，现有绑定基本可复用。 |
| 中 | 需要补充少量类型/API，或处理数据文件、算法边界及互操作参数。 |
| 高 | 涉及多组 Widget 类型、回调、交互生命周期，人工验证成本较大。 |
| 已有 | 仓库已有对应实现；不表示已经接入工程地形数据或完成全部边界验证。 |

难度包含最小可运行示例的实现和验证，不包含完整工程功能开发。表中的绑定缺口为静态检查发现的主要缺口，不是最终候选白名单。

## 直接面向地形建模与处理

| 官方示例 | 内容与工程用途 | 难度/当前状态 | 移植重点 |
| --- | --- | --- | --- |
| [TriangulateTerrainMap](https://examples.vtk.org/site/Cxx/Filtering/TriangulateTerrainMap/) | 生成规则 XY 点格及随机高程，构建地形三角网。 | 已移植 | `Filtering/TriangulateTerrainMap`；统一流程 smoke 通过。 |
| [Delaunay2D](https://examples.vtk.org/site/Cxx/Filtering/Delaunay2D/) | 将带 Z 高程的点集按 XY 投影三角化。 | 已移植 | `Filtering/Delaunay2D`；统一流程 smoke 通过。 |
| [ConstrainedDelaunay2D](https://examples.vtk.org/site/Cxx/Filtering/ConstrainedDelaunay2D/) | 带边界、孔洞约束的三角化，可作为地形边界和断裂线处理基础。 | 已有；地形化改造低到中 | 当前示例是规则点格及孔洞；实际断裂线需保证点索引一致、投影约束不相交。 |
| [DEMReader](https://examples.vtk.org/site/Cxx/IO/DEMReader/) | 读取 DEM，用颜色显示高程栅格。 | 已移植 | `IO/DEMReader`；复用 `SainteHelens.dem`，统一流程 smoke 通过。 |
| [FitToHeightMap](https://examples.vtk.org/site/Cxx/Meshes/FitToHeightMap/) | 将平面网格贴合 DEM，对比点贴合和单元贴合。 | 已有 | 当前包含三个视口、共享相机及 DEM 数据，是现阶段较完整的地形示例。 |
| [GreedyTerrainDecimation](https://examples.vtk.org/site/Cxx/PolyData/GreedyTerrainDecimation/) | 从高程栅格生成地形三角网，使用地形专用简化算法。 | 已移植 | `PolyData/GreedyTerrainDecimation`；复用 3×3 原始栅格，统一流程 smoke 通过；未作真实 DEM 精度评估。 |
| [InterpolateTerrain](https://examples.vtk.org/site/Cxx/PolyData/InterpolateTerrain/) | 对比栅格高程插值与三角网垂直射线求交。 | 已移植 | `PolyData/InterpolateTerrain`；定位器以交点集输出重载适配，统一流程 smoke 通过。 |
| [InterpolateMeshOnGrid](https://examples.vtk.org/site/Cxx/PolyData/InterpolateMeshOnGrid/) | 将散点生成的三角网高程重采样到规则点格。 | 已移植 | `PolyData/InterpolateMeshOnGrid`；统一流程 smoke 通过。 |
| [ResampleAppendedPolyData](https://examples.vtk.org/site/Cxx/PolyData/ResampleAppendedPolyData/) | 对包含多个物体的平面地形进行垂直射线重采样。 | 已移植 | `PolyData/ResampleAppendedPolyData`；probe 分辨率调整为 50×50，统一流程 smoke 通过。 |
| [SmoothMeshGrid](https://examples.vtk.org/site/Python/PolyData/SmoothMeshGrid/)（Python） | 构建网格地形，对比 Loop 与 Butterfly 曲面细分。 | 已移植 | `PolyData/SmoothMeshGrid`；统一流程 smoke 通过。 |
| [LineOnMesh](https://examples.vtk.org/site/Python/DataManipulation/LineOnMesh/)（Python） | 垂直投影采样地形，再以样条连接，生成贴地线。 | 已移植 | `DataManipulation/LineOnMesh`；采样降至 100 点，样条点间不保证贴面，统一流程 smoke 通过。 |
| [Hawaii](https://examples.vtk.org/site/Cxx/Visualization/Hawaii/) | 对真实夏威夷地形进行高程着色。 | 跳过 | 依赖 `honolulu.vtk`；已检查官方 vtk-examples 当前仓库和本机 VTK `Testing/Data` / ExternalData，均未找到。仅找到的下载线索指向旧版 FTP 或第三方镜像，不能满足本例数据来源与版本可追溯要求。 |
| [DecimateHawaii](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/DecimateHawaii/) | 使用 `vtkDecimatePro` 简化真实地形，对比简化效果。 | 跳过 | 与 Hawaii 共用未找到的 `honolulu.vtk`；另外官方源码的“原始” mapper 也连接简化结果，数据缺失时不移植以免更换输入后改变示例目的。 |
| [InteractorStyleTerrain](https://examples.vtk.org/site/Cxx/Interaction/InteractorStyleTerrain/) | 使用地形相机交互方式浏览场景。 | 已有 | 原例显示球体，演示相机操作；可直接用于地形场景。 |

## 可复用于地形的通用处理

部分原始输入是球面或普通网格，接入地形数据还需要调整。已有通用示例与完成地形功能应分别记录。

| 官方示例 | 地形应用 | 难度/当前状态 | 移植重点 |
| --- | --- | --- | --- |
| [ColoredElevationMap](https://examples.vtk.org/site/Cxx/Meshes/ColoredElevationMap/) | 按顶点 Z 高程着色。 | 已移植 | `Meshes/ColoredElevationMap`；统一流程 smoke 通过。 |
| [ElevationFilter](https://examples.vtk.org/site/Cxx/Meshes/ElevationFilter/) | 沿指定方向生成高程标量并着色。 | 已移植 | `Meshes/ElevationFilter`；明确标量范围 0–9，统一流程 smoke 通过。 |
| [ContoursFromPolyData](https://examples.vtk.org/site/Cxx/Filtering/ContoursFromPolyData/) | 从地形高程标量提取等高线。 | 已移植 | `Filtering/ContoursFromPolyData`；采用官方无文件参数的球面输入，统一流程 smoke 通过。 |
| [LabelContours](https://examples.vtk.org/site/Cxx/Visualization/LabelContours/) | 等高线及高程标签。 | 已有；接入地形低 | 核查标签值、高程间隔及显示遮挡。 |
| [FilledContours](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/FilledContours/) | 分层设色、填充高程区间。 | 跳过 | 原例依赖多级裁剪、单元标量与区间边界管理；本次未补齐这组 API，优先完成其他离散色带与等值线示例。 |
| [Cutter](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/Cutter/) | 以竖直平面截取地形剖面。 | 已有；接入地形低 | 截线可能包含多段，输出工程断面还需排序和累计里程。 |
| [QuadricDecimation](https://examples.vtk.org/site/Cxx/Meshes/QuadricDecimation/) | 减少地形三角形数量。 | 已有；接入地形低 | 当前使用球面；地形应用需检查高程误差及边界保持。 |
| [WindowedSincPolyDataFilter](https://examples.vtk.org/site/Cxx/Meshes/WindowedSincPolyDataFilter/) | 地形曲面平滑。 | 已有；接入地形低 | 平滑会改变测量高程，需要限制边界、特征及允许误差。 |
| [PointInterpolator](https://examples.vtk.org/site/Cxx/Meshes/PointInterpolator/) | 将散点标量插值到网格，可用于高程或工程属性场。 | 已有；高程建模改造中 | 当前是属性插值到 STL；生成地形需使用平面目标网格，再将高程标量转为 Z。 |
| [DijkstraGraphGeodesicPath](https://examples.vtk.org/site/Cxx/PolyData/DijkstraGraphGeodesicPath/) | 沿地形网格边寻找最短路径。 | 已移植 | `PolyData/DijkstraGraphGeodesicPath`；统一流程 smoke 通过；路径依赖网格边，不代表连续曲面最短路径。 |
| [PolygonalSurfacePointPlacer](https://examples.vtk.org/site/Cxx/PolyData/PolygonalSurfacePointPlacer/) | 在地形表面交互放置控制点。 | 跳过 | 需要一组 PointPlacer、ContourWidget/Representation、拾取器及事件生命周期绑定；超出单个示例可安全核验的最小 API 范围。 |
| [PolygonalSurfaceContourLineInterpolator](https://examples.vtk.org/site/Cxx/PolyData/PolygonalSurfaceContourLineInterpolator/) | 在地形表面交互绘制、编辑曲线。 | 跳过 | 依赖整组表面约束 Widget 与交互回调；需在窗口环境中核验拖动、拾取、对象保活及销毁，按用户允许的高难 API 跳过。 |

## 研究参考

| 官方示例 | 难度 | 适用范围与限制 |
| --- | --- | --- |
| [ShepardInterpolation](https://examples.vtk.org/site/Cxx/Visualization/ShepardInterpolation/) | 跳过 | 原例以三维体采样展示反距离加权；转换为二维地形需要改变采样维数、数据域与可视化管线，本轮优先处理直接面向地形的网格算法。 |
| [ContoursToSurface](https://examples.vtk.org/site/Cxx/PolyData/ContoursToSurface/) | 跳过 | 依赖专用 `vtkVoxelContoursToSurfaceFilter`，并假设多层闭合轮廓生成封闭体；它不是测量等高线构成开放地形的直接替代。 |

## 已有实现与资源

以下路径相对于仓库根目录，可作为后续移植的复用入口：

- `src/examples/ExampleBrowser/Examples/Meshes/FitToHeightMap/`：DEM 读取、曲面生成、贴合及多视口显示。
- `src/examples/ExampleBrowser/Examples/Meshes/FitToHeightMap/Data/SainteHelens.dem`：已有地形数据；来源与验证记录见同目录的 `porting-notes.md`。
- `src/examples/ExampleBrowser/Examples/Filtering/ConstrainedDelaunay2D/`：边界和孔洞约束三角化。
- `src/examples/ExampleBrowser/Examples/Interaction/InteractorStyleTerrain/`：地形相机交互。
- `src/examples/ExampleBrowser/Examples/Meshes/PointInterpolator/`：稀疏采样值向表面插值。
- `src/examples/ExampleBrowser/Examples/Visualization/LabelContours/`：等值线及标注。
- `src/examples/ExampleBrowser/Examples/VisualizationAlgorithms/Cutter/`：平面截线。
- `src/examples/ExampleBrowser/Examples/Meshes/QuadricDecimation/`、`WindowedSincPolyDataFilter/`：网格简化和平滑。

## 推荐实施顺序

1. **TIN 建模与显示**：优先移植 `TriangulateTerrainMap` 和 `ColoredElevationMap`。`Delaunay2D` 与前者重叠，可按学习需要补充。
2. **高程查询与重采样**：移植 `InterpolateMeshOnGrid` 和 `InterpolateTerrain`，优先解决 CellLocator 及射线求交绑定。
3. **地形简化**：复用已移植的 `GreedyTerrainDecimation`；`DecimateHawaii` 因官方 `honolulu.vtk` 数据无法取得且原例管线连接存在问题，已跳过。真实 DEM 上的误差对比仍待开展。
4. **等高线与剖面**：复用已有 `LabelContours`、`Cutter` 串联 DEM 或 TIN 管线；复杂分段裁剪 `FilledContours` 已按难移植管线跳过。
5. **贴地线与交互编辑**：已移植 `LineOnMesh`；表面 PointPlacer 和 ContourLineInterpolator 依赖完整 Widget 生命周期与交互验收，已跳过。

## 工程验证重点

- **地形表达**：上述 TIN 和 DEM 管线主要面向单值高度场 `z = f(x, y)`。同一 XY 对应多个高程、悬挑或洞穴时，应保留三维曲面表达，不能直接压缩为高度场。
- **坐标与单位**：明确水平坐标系、高程基准、水平与高程单位；大坐标场景检查精度，必要时采用局部坐标。
- **建模输入**：检查空数据、单点、共线点、重复或近重复 XY、退化三角形、边界和断裂线约束。
- **采样与缺测**：检查 NoData、孔洞、边界外查询和射线未命中；不要把无效样本当作零高程。栅格与 TIN 的插值结果可能不同，应按应用语义选用。
- **简化与平滑**：除三角形数量外，还应统计高程误差，检查边界、山脊、沟谷和断裂线；细分不等于增加测量精度。
- **贴地线**：检查采样密度、跨孔洞和样条插值后的离地误差；必要时对曲线重新采样并投影。
- **互操作与交互**：核对射线求交的数组长度、输出参数和对象所有权；Widget 验证事件委托保活、拖拽、拾取及重复创建/销毁。
- **数据与验收**：固定官方源码 revision，逐文件记录数据来源；`honolulu.vtk` 尚未在本次评估中准备。移植后按[统一验证流程](../workflow/verification.md)检查生成一致性、构建及目标示例截图，并人工检查交互。
