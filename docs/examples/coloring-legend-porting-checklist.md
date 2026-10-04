# 模型着色与图例示例移植清单

本文整理 VTK 官方示例中与模型顶点着色、单元着色和图例相关的示例，并基于当前 VtkSharp 绑定及 ExampleBrowser 评估移植难度。

- 清单整理日期：2026-10-03；本轮移植记录：2026-10-04。
- 官方入口：[C++ 示例目录](https://examples.vtk.org/site/Cxx/)。相同示例的多语言版本不重复列入清单。
- 移植目标：当前 C# ExampleBrowser，包括必要的绑定补充、数据准备和验证。
- 初始评估依据：官方页面源码、当时的生成绑定和已有示例实现。
- 本轮按示例检查缺失 API；新增绑定通过候选白名单流程。各示例的翻译差异、验证报告和未完成的人工检查记录在对应 `porting-notes.md`。

## 着色方式与工程语义

颜色的作用对象与颜色来源是两个独立维度。

| 作用对象 | 常用方式 | 工程用途 |
| --- | --- | --- |
| 整个模型或对象 | `vtkActor.GetProperty().SetColor(...)`；使用统一颜色时关闭 Mapper 的标量着色。 | 零件、图层、选中对象统一颜色。 |
| 每个顶点 | `PointData`，Mapper 选择点数据。 | 节点温度、位移、连续云图。 |
| 每个单元 | `CellData`，Mapper 选择单元数据。 | 单元应力、材料编号、网格质量、不同杆件颜色。 |
| 模型边线 | `EdgeVisibilityOn()`、`SetEdgeColor(...)`。 | 云图叠加统一颜色的网格边线。 |
| 顶点标记 | `VertexVisibilityOn()`、`SetVertexColor(...)`。 | 显示网格节点，统一设置节点标记颜色。 |

PointData 和 CellData 都可以保存直接 RGB/RGBA，或保存标量再通过 LUT/颜色传递函数映射。前者适合指定颜色，后者保留数值含义，便于显示数值色标。

节点着色通常在面内插值；单元着色通常让每个单元使用一个颜色。将单元结果转换为节点结果需要明确平均或投影规则，不能仅为显示平滑而改变工程结果的含义。

`SetEdgeColor` 通常统一作用于一个 Actor 的边线。每条边需要独立颜色时，可将边提取为线单元，再用 CellData 着色。顶点标记颜色与用于表面插值的 PointData 颜色也应区分。

接口依据：[vtkMapper](https://vtk.org/doc/nightly/html/vtkMapper_8h_source.html)、[vtkProperty](https://vtk.org/doc/nightly/html/classvtkProperty.html)。纹理、体渲染和自定义 Shader 属于后续扩展方向，本清单暂不展开。

## 难度与状态说明

| 标记 | 含义 |
| --- | --- |
| 低 | 主要是 C# 翻译，现有绑定基本可复用，可能需要少量方法补充。 |
| 中 | 需要补充类型/API，或准备数据、处理互操作参数及交互验证。 |
| 高 | 涉及较复杂管线、多组绑定、数值处理或较多交互，验证成本较大。 |
| 已有 | 仓库已有对应实现；不表示已完成所有工程边界条件验证。 |

难度包含最小可运行示例的实现和验证，不包含完整工程功能开发。`[x]` 表示已移植，`跳过` 行注明了特殊语法/API 的限制；仍未勾选的项目待移植。

## 顶点 RGB 与点云着色

| 状态 | 官方示例 | 内容与工程用途 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [x] | [TriangleColoredPoints](https://examples.vtk.org/site/Cxx/PolyData/TriangleColoredPoints/) | 三角形三个顶点分别设置 RGB，观察面内颜色插值。 | 低，首批 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/PolyData/TriangleColoredPoints/`。 |
| [x] | [ColoredPoints](https://examples.vtk.org/site/Cxx/PolyData/ColoredPoints/) | 三个独立点分别设置 RGB，作为点云着色的最小示例。 | 低 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/PolyData/ColoredPoints/`。 |
| [x] | [ColoredElevationMap](https://examples.vtk.org/site/Cxx/Meshes/ColoredElevationMap/) | 生成地形三角网，根据顶点 Z 高程查表，再写入 RGB。 | 低，首批 | 复用地形清单已移植实现 `Meshes/ColoredElevationMap`。 |

## 顶点标量与颜色映射

| 状态 | 官方示例 | 内容与工程用途 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [x] | [ColoredSphere](https://examples.vtk.org/site/Cxx/Rendering/ColoredSphere/) | 球面通过高程过滤器生成标量并着色。 | 中 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/Rendering/ColoredSphere/`。 |
| [x] | [ElevationFilter](https://examples.vtk.org/site/Cxx/Meshes/ElevationFilter/) | 沿指定低点—高点方向生成顶点高程标量。 | 中 | 复用地形清单已移植实现 `Meshes/ElevationFilter`。 |
| [x] | [SimpleElevationFilter](https://examples.vtk.org/site/Cxx/Meshes/SimpleElevationFilter/) | 根据位置与指定向量生成高程标量。 | 中 | 已移植并补 `SetVector(double,double,double)`；保留官方整数高度和向量投影语义，验证见 `src/examples/ExampleBrowser/Examples/Meshes/SimpleElevationFilter/`。 |
| [x] | [ColorMapToLUT](https://examples.vtk.org/site/Cxx/Utilities/ColorMapToLUT/) | 自定义颜色传递函数，比较连续、离散及反向配色。 | 中，第二批 | 已移植 Fast 调色板的离散 Lab 模式；浏览器示例固定圆锥和调色选项，差异见 `src/examples/ExampleBrowser/Examples/Utilities/ColorMapToLUT/`。 |
| 跳过 | [Curvatures](https://examples.vtk.org/site/Cxx/Visualization/Curvatures/) | 按模型曲率着色并显示色标。 | 中到高 | 官方实现包含邻域拓扑遍历与边缘曲率修正、CLI11/Filesystem、版本宏和不同 VTK 版本的 ID 过滤器分支；完整移植需超出单一着色示例范围。源码细节见官方 C++ 页面。 |

## 单元、面与线着色

| 状态 | 官方示例 | 内容与工程用途 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [x] | [ColorCells](https://examples.vtk.org/site/Cxx/PolyData/ColorCells/) | 为平面网格的各单元赋标量，通过 LUT 映射颜色。 | 低，首批 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/PolyData/ColorCells/`。 |
| [x] | [ColorCellsWithRGB](https://examples.vtk.org/site/Cxx/PolyData/ColorCellsWithRGB/) | 各单元直接指定 RGB。 | 低，首批 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/PolyData/ColorCellsWithRGB/`。 |

已有 [ColoredLines](https://examples.vtk.org/site/Cxx/GeometricObjects/ColoredLines/)：通过 CellData 分别设置两条线的颜色，可作为杆件、边线独立着色的参考。当前实现见 [ColoredLines.cs](../../src/examples/ExampleBrowser/Examples/GeometricObjects/ColoredLines/ColoredLines.cs)。

## 色标、分类图例与比例尺

| 状态 | 官方示例 | 内容与工程用途 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [x] | [ScalarBarActor](https://examples.vtk.org/site/Cxx/Visualization/ScalarBarActor/) | 球面顶点标量着色与连续数值色标。 | 低，首批 | 已移植；验证与说明见 `src/examples/ExampleBrowser/Examples/Visualization/ScalarBarActor/`。 |
| [x] | [ScalarBarActorColorSeries](https://examples.vtk.org/site/Cxx/Visualization/ScalarBarActorColorSeries/) | 使用预设颜色系列着色球面并显示色标。 | 低，首批 | 已移植；枚举值适配和验证见 `src/examples/ExampleBrowser/Examples/Visualization/ScalarBarActorColorSeries/`。 |
| [x] | [ScalarBarWidget](https://examples.vtk.org/site/Cxx/Widgets/ScalarBarWidget/) | 可交互移动、调整的水平色标。 | 中，首批 | 已移植，使用程序生成球面替换缺失的 `.vtk` 数据；验证与交互限制见 `src/examples/ExampleBrowser/Examples/Visualization/ScalarBarWidget/`。 |
| [x] | [Legend](https://examples.vtk.org/site/Cxx/Visualization/Legend/) | Box / Ball 的对象名称、颜色及符号图例。 | 中，首批 | 已移植；通过 `vtkPolyData` 保持源几何至渲染结束，颜色使用三分量数组；验证见 `src/examples/ExampleBrowser/Examples/Visualization/Legend/`。 |
| [x] | [LegendScaleActor](https://examples.vtk.org/site/Cxx/Annotation/LegendScaleActor/) | 场景比例尺和四周刻度轴。 | 低 | 已移植；以球体展示场景范围，验证见 `src/examples/ExampleBrowser/Examples/Visualization/LegendScaleActor/`。 |
| 跳过 | [RescaleReverseLUT](https://examples.vtk.org/site/Cxx/Utilities/RescaleReverseLUT/) | 多视口比较色表范围重映射、反向配色及色标。 | 中到高，第二批 | 原例依赖多个联动 viewport、范围重映射、反向 LUT 和文本注释 Widget；目前多组映射/注释 API 未封装，无法在本次清单内保持各视口与色标对应关系并完成交互验收。 |

## 分带云图与综合后处理

| 状态 | 官方示例 | 内容与工程用途 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [x] | [BandedPolyDataContourFilter](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/BandedPolyDataContourFilter/) | 将顶点标量切分成离散色带，并绘制等值边界。 | 中，第二批 | 已移植；补充过滤器及 `GenerateValues` 绑定，输出按 CellData 映射；验证见 `src/examples/ExampleBrowser/Examples/VisualizationAlgorithms/BandedPolyDataContourFilter/`。 |
| 跳过 | [FilledContours](https://examples.vtk.org/site/Cxx/VisualizationAlgorithms/FilledContours/) | 通过分段裁剪构造填色等值区域。 | 中到高 | 依赖裁剪输出的 CellData 区间和边界清理，并要求填充区间端点正确处理；属于多阶段裁剪/拓扑处理，按难移植的专用管线跳过。原例还使用外部 `.vtp` 数据。 |
| 跳过 | [ElevationBandsWithGlyphs](https://examples.vtk.org/site/Cxx/Visualization/ElevationBandsWithGlyphs/) | 高程色带、法向 Glyph 和带注释色标。 | 高，后续 | 色带注释依赖 `vtkVariant` / `vtkVariantArray` 的区间值传递，且同时组合 Glyph 与标注色标；当前 Variant 参数 ABI 未封装，按特殊 API 跳过。 |
| 跳过 | [CurvatureBandsWithGlyphs](https://examples.vtk.org/site/Cxx/Visualization/CurvatureBandsWithGlyphs/) | 曲率色带、Glyph、统计与色标。 | 高，后续 | 依赖上项 Variant 色带注释 API，并增加曲率统计、邻域边界数值处理和 Glyph 管线；按特殊 API 组合跳过。 |

## 当前可复用能力与共用缺口

以下路径相对于仓库根目录：

- `src/bindings/VtkSharp/` 已有 `vtkLookupTable`、`vtkColorTransferFunction`、`vtkDiscretizableColorTransferFunction` 和 `vtkColorSeries`。
- 已有 `vtkScalarBarActor`、`vtkScalarBarWidget`、`vtkScalarBarRepresentation` 和 `vtkLegendScaleActor`。
- `src/bindings/VtkSharp.Tests/VtkColorMappingBindingsTests.cs`、`VtkScalarBarBindingsTests.cs` 提供现有绑定验证参考；不能代替待移植示例的渲染验收。
- `vtkMapper` 当前尚未导出 `SetColorModeToMapScalars`、`GetLookupTable`、`InterpolateScalarsBeforeMappingOn`；本轮固定配色示例未依赖这些可选控制，因此未扩大绑定范围。
- `vtkScalarsToColors` 已有 indexed lookup 开关，但尚未导出 `SetAnnotation` / `GetAnnotation`；复杂分类注释仍需规划。
- 当前尚缺 `vtkCurvatures` 等类型；`vtkElevationFilter`、`vtkSimpleElevationFilter`、`vtkLegendBoxActor` 与 `vtkBandedPolyDataContourFilter` 已通过本轮所需的最小白名单/API 补充。

后续只补目标示例所需 API。采用候选白名单流程定位声明类、选择实际签名并核对方向、长度、所有权，不手改生成文件。

## 推荐实施顺序

1. `TriangleColoredPoints` → `ColorCellsWithRGB` → `ColorCells`：对比顶点与单元、直接颜色与标量映射。
2. `ScalarBarActor` → `ColoredElevationMap` → `ScalarBarActorColorSeries`：建立基础云图、数值色标和预设配色。
3. `Legend` → `ScalarBarWidget`：补齐对象分类图例和色标交互。
4. `ColorMapToLUT` → `BandedPolyDataContourFilter` → `RescaleReverseLUT`：扩展自定义色表、分带和范围调整。
5. 根据工程用途选择曲率、填色等值区域及 Glyph 综合示例；`LegendScaleActor` 可独立安排。

`ColoredSphere`、`ElevationFilter`、`SimpleElevationFilter` 的内容有重叠，可按需要选择；`ColorMapToLUT` 同样会带动高程过滤器绑定补充。部分示例也列在[地形示例清单](terrain-porting-checklist.md)中，移植后应同步状态。

## 验证重点

- 颜色/标量元组数与点数或单元数一致；Mapper 使用预期的数据关联和数组。
- 模型与数值色标共享 LUT/颜色传递函数，并统一有效映射范围；不能仅共享对象而忽略 Mapper 自身的范围设置。
- 验证常量场、极小范围、负值、超范围值和 NaN；直接 RGB 不自动具备可解释的数值色标。
- 高程过滤器输出可能经过归一化，图例应明确显示物理值还是归一化值。官方示例中的额外 Colors 数组不一定是 Mapper 实际选用的数组。
- 分带显示验证区间端点、空区间及退化面；节点与单元结果转换保留明确的工程语义。
- Widget 验证拖拽、缩放、窗口大小变化和关闭时的对象释放；图例验证文字、布局和高 DPI。
- 按[统一验证流程](../workflow/verification.md)完成构建、生成一致性检查和目标示例验收；记录数据来源、翻译差异及人工确认结果。

## 相关参考但不列入模型着色待办

- [ColorVerticesLookupTable](https://examples.vtk.org/site/Cxx/Graphs/ColorVerticesLookupTable/) 着色的是图节点，属于 Graphs 管线，不是三角网格顶点。
- 整体颜色、统一边线颜色和顶点标记颜色可在上述最小示例上验证；本次没有为它们新增独立官方示例待办。
