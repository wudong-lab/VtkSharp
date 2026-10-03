# 鼠标、键盘、选择与高亮示例移植清单

本文整理 VTK 官方示例中与鼠标、键盘交互、图元选择和高亮相关的示例，并基于当前 VtkSharp 绑定和 ExampleBrowser 评估移植难度。

- 评估日期：2026-10-03。
- 官方入口：[C++ 示例目录](https://examples.vtk.org/site/Cxx/)。相同示例的多语言版本不重复列入清单。
- 移植目标：当前 C# ExampleBrowser，包括必要的绑定补充、数据准备和验证；允许用托管 observer 替代 C++ 交互器子类。
- 评估依据：官方页面源码、当前生成绑定和已有示例实现；尚未对全部调用执行 `plan-bindings`，也未构建或运行待移植示例。
- 表中的绑定缺口是静态检查发现的主要缺口，不是最终候选白名单。移植前应重新检查当前代码、官方源码 revision 和数据来源。

## 选择对象与工程语义

| 选择层级 | 含义 | 工程用途 |
| --- | --- | --- |
| Actor / Prop | 拾取场景中的显示对象。一个 Actor 可以包含多个业务实体。 | 选择零件、对象或整组网格。 |
| Point | 拾取或提取数据集中的点。 | 选择网格节点、点云点或编辑顶点。 |
| Cell | 拾取或提取数据集中的单元，包括线、面和体单元。 | 选择杆件、三角面或有限元单元。 |
| 世界坐标 | 取得鼠标位置对应的三维坐标，不一定提供图元身份。 | 定位、测量、交互输入。 |

VTK 的 cell 不等同于 CAD 拓扑面。将选择结果映射回工程对象，需要保留业务 ID；过滤器输出的 point/cell ID 可能与输入不同。

框选还需区分：

- **穿透框选**：按选择视锥提取数据，可能包含被遮挡的背面单元。`HighlightSelection` 属于这一类。
- **可见选择**：根据实际渲染和遮挡关系取得屏幕可见数据。参考 `ExtractVisibleCells` 和 `SelectVisiblePoints`。

官方依据：[HighlightSelection](https://examples.vtk.org/site/Cxx/Picking/HighlightSelection/)、[ExtractVisibleCells](https://examples.vtk.org/site/Cxx/Filtering/ExtractVisibleCells/)。

## 难度与状态说明

| 标记 | 含义 |
| --- | --- |
| 低 | 主要是 C# 翻译，现有基础能力可复用，可能需要少量方法补充。 |
| 中 | 需要补充类型/API，或调整事件处理方式、互操作参数及验证。 |
| 高 | 涉及交互状态、C++ protected 成员、几何编辑或较复杂互操作，验证成本较大。 |
| 已有 | 仓库已有对应实现；不表示与官方行为完全一致或已完成交互验收。 |

难度包含最小可运行示例的实现和验证，不包含完整工程功能开发。未勾选项均待移植；已有项仅作为复用参考。

## 鼠标、键盘与交互模式

| 状态 | 官方示例 | 内容 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [ ] | [MouseEventsObserver](https://examples.vtk.org/site/Cxx/Interaction/MouseEventsObserver/) | 用 observer 接收鼠标按键、键盘事件。 | 低，首批 | 使用现有 `AddObserver`，验证事件触发与默认相机操作。 |
| [ ] | [MouseEvents](https://examples.vtk.org/site/Cxx/Interaction/MouseEvents/) | 重写左、中、右键处理函数。 | 中 | 转为 observer 后保留默认操作，避免重复转发事件。 |
| [ ] | [KeypressObserver](https://examples.vtk.org/site/Cxx/Interaction/KeypressObserver/) | 用回调读取按键。 | 低，首批 | 已有 `GetKeySym()`。 |
| [ ] | [KeypressEvents](https://examples.vtk.org/site/Cxx/Interaction/KeypressEvents/) | 重写按键处理，识别方向键和字符键。 | 中 | 协调自定义操作与默认快捷键。 |
| [ ] | [ShiftAndControl](https://examples.vtk.org/site/Cxx/Interaction/ShiftAndControl/) | 检测鼠标操作时的 Shift、Ctrl 状态。 | 中，首批 | 状态读取已有；原例依赖尚未绑定的 TrackballActor。 |
| [ ] | [DoubleClick](https://examples.vtk.org/site/Cxx/Interaction/DoubleClick/) | 根据点击次数和位置判断双击。 | 低 | 原例没有时间阈值；工程应用需明确采用宿主双击事件还是补充时间判断。 |
| [ ] | [TrackballCamera](https://examples.vtk.org/site/Cxx/Interaction/TrackballCamera/) | 鼠标旋转、平移、缩放相机。 | 低 | 已有对应 style 绑定。 |
| [ ] | [TrackballActor](https://examples.vtk.org/site/Cxx/Interaction/TrackballActor/) | 鼠标旋转、移动、缩放 Actor。 | 中 | 需补 `vtkInteractorStyleTrackballActor`。 |
| [ ] | [StyleSwitch](https://examples.vtk.org/site/Cxx/Interaction/StyleSwitch/) | 切换相机/Actor、Trackball/Joystick 模式。 | 中 | 已有类型；具体切换方法需补充。 |
| [ ] | [InteractorStyleUser](https://examples.vtk.org/site/Cxx/Interaction/InteractorStyleUser/) | 使用不提供默认操作的自定义交互样式。 | 中 | 需补类型，适合验证自定义输入处理。 |
| [ ] | [RubberBandZoom](https://examples.vtk.org/site/Cxx/Interaction/RubberBandZoom/) | 拉框缩放视图。 | 低 | 已有对应类型。 |
| [x] | [InteractorStyleTerrain](https://examples.vtk.org/site/Cxx/Interaction/InteractorStyleTerrain/) | 地形浏览式相机操作。 | 已有 | 当前示例显示球体，演示相机交互。 |

## 图元拾取与框选

| 状态 | 官方示例 | 内容 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [ ] | [Picking](https://examples.vtk.org/site/Cxx/Interaction/Picking/) | 点击取得 Actor 和世界坐标。 | 中，第二批 | `vtkPropPicker` 已有；需补拾取坐标接口。 |
| [ ] | [WorldPointPicker](https://examples.vtk.org/site/Cxx/Interaction/WorldPointPicker/) | 将鼠标位置转换为世界坐标。 | 中 | 需补 picker 类型；不提供图元身份。 |
| [ ] | [PointPicker](https://examples.vtk.org/site/Cxx/Interaction/PointPicker/) | 拾取鼠标附近的数据点。 | 中 | 需补 `vtkPointPicker` 和必要方法，验证拾取容差。 |
| [x] | [CellPicking](https://examples.vtk.org/site/Cxx/Picking/CellPicking/) | 点击单元、提取并高亮。 | 已有 | 当前实现累计追加 ID，与官方每次提取单个命中单元的行为不同。 |
| [ ] | [SelectAnActor](https://examples.vtk.org/site/Cxx/Interaction/SelectAnActor/) | 区分选中的是立方体还是球体。 | 中到高 | 原例访问 protected `InteractionProp`；建议改为显式 picker，核对与 Actor 操作的关系。 |
| [ ] | [PickableOff](https://examples.vtk.org/site/Cxx/Interaction/PickableOff/) | 排除某个 Actor 的拾取和交互。 | 中 | `PickableOff()` 已有；原例需 TrackballActor。 |
| [ ] | [AreaPicking](https://examples.vtk.org/site/Cxx/Picking/AreaPicking/) | 框选并枚举命中的 Prop。 | 中，第三批 | 需补 `vtkAreaPicker` 和结果集合遍历；观察 `EndPickEvent`。 |
| [ ] | [RubberBandPick](https://examples.vtk.org/site/Cxx/Interaction/RubberBandPick/) | 演示橡皮筋选择交互模式。 | 低 | 已有类型；单独使用不完成数据提取和高亮。 |
| [ ] | [RubberBand2DObserver](https://examples.vtk.org/site/Cxx/Interaction/RubberBand2DObserver/) | 通过事件取得框选矩形。 | 中，第三批 | `SelectionChangedEvent` 的 `callData` 为 native 矩形数组，须在回调内读取或复制。 |
| [ ] | [RubberBand2D](https://examples.vtk.org/site/Cxx/Interaction/RubberBand2D/) | 重写鼠标释放事件，读取框选起止位置。 | 中 | 原例访问 protected 位置成员；优先采用 Observer 版本。 |
| [ ] | [RubberBand3D](https://examples.vtk.org/site/Cxx/Interaction/RubberBand3D/) | 在三维视图中进行拉框交互。 | 中到高 | 需补类型；仍需另外实现选择数据处理。 |

## 高亮与选择结果处理

| 状态 | 官方示例 | 内容 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [ ] | [HighlightPickedActor](https://examples.vtk.org/site/Cxx/Picking/HighlightPickedActor/) | 修改命中 Actor 的颜色和边显示，恢复上次属性。 | 中，第二批 | picker 已有；需补属性 `DeepCopy` 等方法，保存并恢复原状态。 |
| [ ] | [HighlightWithSilhouette](https://examples.vtk.org/site/Cxx/Picking/HighlightWithSilhouette/) | 用轮廓线高亮选中 Actor。 | 中，第二批 | 需补 `vtkPolyDataSilhouette`；工程扩展需处理 Actor 变换与相机变化。 |
| [ ] | [HighlightSelectedPoints](https://examples.vtk.org/site/Cxx/Picking/HighlightSelectedPoints/) | 框选点、红色显示并输出原始 ID。 | 中到高，第三批 | AreaPicker、视锥提取、ID 保留；`vtkGenerateIds`、`vtkVertexGlyphFilter` 等已有。 |
| [ ] | [HighlightSelection](https://examples.vtk.org/site/Cxx/Picking/HighlightSelection/) | 框选单元，以线框高亮。 | 中到高，第三批 | 需补 `vtkExtractPolyDataGeometry` 等；原例访问 protected `CurrentMode`，可评估以 picker 事件驱动。无参数时生成球体，无须先准备外部模型。 |
| [ ] | [ExtractVisibleCells](https://examples.vtk.org/site/Cxx/Filtering/ExtractVisibleCells/) | 用硬件选择取得屏幕可见单元。 | 高，第四批 | 缺 `vtkHardwareSelector`、`vtkSelection` 等；返回对象所有权和实际渲染环境需验证。 |
| [ ] | [SelectVisiblePoints](https://examples.vtk.org/site/Cxx/PolyData/SelectVisiblePoints/) | 根据深度缓冲筛出可见点。 | 中 | 已有类型；需核对方法，验证遮挡和深度容差。 |
| [ ] | [ExtractSelection](https://examples.vtk.org/site/Cxx/PolyData/ExtractSelection/) | 按 ID 提取选中和未选中数据。 | 中 | 可采用现有 `vtkSelectionSource`；反选能力需补充，避免直接照搬静态信息键调用。 |
| [x] | [ExtractSelectionOriginalId](https://examples.vtk.org/site/Cxx/PolyData/ExtractSelectionOriginalId/) | 提取后追溯原始点 ID。 | 已有 | 可作为选择结果与业务对象映射的参考。 |
| [x] | [HighlightBadCells](https://examples.vtk.org/site/Cxx/PolyData/HighlightBadCells/) | 按网格质量筛选并高亮单元。 | 已有 | 属于条件选择，可复用叠加显示方式。 |

## 拖动编辑与二维图像扩展

| 状态 | 官方示例 | 内容 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [ ] | [SelectAVertex](https://examples.vtk.org/site/Cxx/Interaction/SelectAVertex/) | 选中并拖动 PolyData 顶点。 | 高，第四批 | 拾取、拖动状态、屏幕/世界坐标转换和数据更新。 |
| [ ] | [MoveAVertexUnstructuredGrid](https://examples.vtk.org/site/Cxx/Interaction/MoveAVertexUnstructuredGrid/) | 拖动非结构网格顶点。 | 高，第四批 | 更新底层点及显示管线；工程应用需增加几何有效性约束。 |
| [ ] | [MoveAGlyph](https://examples.vtk.org/site/Cxx/Interaction/MoveAGlyph/) | 拾取并拖动单个 glyph。 | 高，第四批 | glyph 输出 ID 与输入点映射，以及交互器 protected 状态。 |
| [ ] | [ImageRegion](https://examples.vtk.org/site/Cxx/Interaction/ImageRegion/) | 用矩形 widget 选择图像区域。 | 高，扩展 | widget/representation 绑定、坐标转换和图像输入。 |
| [ ] | [ImageClip](https://examples.vtk.org/site/Cxx/Interaction/ImageClip/) | 交互式选择并裁剪图像。 | 高，扩展 | 图像 extent、裁剪管线、widget 回调和图像输入。 |
| [ ] | [PickPixel](https://examples.vtk.org/site/Cxx/Images/PickPixel/) | 鼠标移动时显示像素插值值。 | 高，扩展 | 图像拾取、数组插值、assembly 路径遍历和图像输入。 |
| [ ] | [PickPixel2](https://examples.vtk.org/site/Cxx/Images/PickPixel2/) | 鼠标移动时读取像素值。 | 高，扩展 | native 标量指针与不同像素类型；可评估采用原例自生成图像路径。 |

## 事件机制配套示例

这些示例用于理解回调、事件数据和对象保活，可与输入交互示例一起实施。

| 状态 | 官方示例 | 内容 | 难度 | 移植重点 |
| --- | --- | --- | --- | --- |
| [ ] | [CallBack](https://examples.vtk.org/site/Cxx/Interaction/CallBack/) | 在交互结束回调中读取相机状态。 | 中 | 事件核心可用委托；完整原例还包含方向标记 widget 和显示管线。 |
| [x] | [ClientData](https://examples.vtk.org/site/Cxx/Interaction/ClientData/) | 给 observer 传递对象上下文。 | 已有 | 可复用托管 `clientData` 和保活方式。 |
| [ ] | [ObserverMemberFunction](https://examples.vtk.org/site/Cxx/Interaction/ObserverMemberFunction/) | 使用对象成员函数作为事件处理器。 | 低 | 用 C# 实例方法委托实现。 |
| [ ] | [CallData](https://examples.vtk.org/site/Cxx/Interaction/CallData/) | 通过事件传递数值。 | 中 | 原例含自定义 native filter；若改为托管事件演示，需记录差异并明确数据有效期。 |
| [ ] | [UserEvent](https://examples.vtk.org/site/Cxx/Interaction/UserEvent/) | 定义、触发和观察自定义事件。 | 中 | 原例含自定义 native filter；核心行为可用现有 `InvokeEvent` 演示。 |

## 推荐实施顺序

1. **输入基础**：`MouseEventsObserver` → `KeypressObserver` → `ShiftAndControl`。建立鼠标位置、按键与修饰键的最小示例。
2. **单选与高亮**：`Picking` → `HighlightPickedActor` → `HighlightWithSilhouette`；复用已有 `CellPicking` 和原始 ID 示例。
3. **框选**：`RubberBand2DObserver` → `AreaPicking` → `HighlightSelectedPoints` → `HighlightSelection`。区分 Actor 选择与 point/cell 提取。
4. **工程选择与编辑**：`ExtractVisibleCells` → 顶点拖动 → glyph 拖动。先验证可见选择，再处理编辑状态和几何更新。
5. **二维图像扩展**：按实际业务需求选择区域 widget、裁剪和像素拾取示例。

相近示例不必同时首批移植。例如鼠标、键盘和二维框选均优先采用 Observer 版本，子类版本用于后续比较行为。

## 当前可复用实现与差异

- [托管 observer](../../src/bindings/VtkSharp/vtkCommonCore/vtkObject.cs)：支持事件、委托、`clientData`、`callData` 和 priority；observer handle 负责移除观察者及释放保活状态。当前没有可直接控制事件中止的托管接口。
- [鼠标位置读取](../../src/bindings/VtkSharp/vtkRenderingCore/vtkRenderWindowInteractor.cs)：已有当前和上次事件位置读取；键盘状态读取在生成绑定中。
- [CellPicking](../../src/examples/ExampleBrowser/Examples/Picking/CellPicking/CellPicking.cs)：复用 CellPicker、SelectionSource、ExtractSelection 和高亮 Actor。当前每次点击追加 ID，属于累计选择；官方原例每次只提取本次命中单元。当前输出仅有 cell ID，世界坐标输出仍需完善。
- [ExtractSelectionOriginalId](../../src/examples/ExampleBrowser/Examples/PolyData/ExtractSelectionOriginalId/ExtractSelectionOriginalId.cs)：复用原始点 ID 追溯和多视口显示。
- [HighlightBadCells](../../src/examples/ExampleBrowser/Examples/PolyData/HighlightBadCells/HighlightBadCells.cs)：复用提取子集后的线框叠加显示。
- [ClientData](../../src/examples/ExampleBrowser/Examples/Interaction/ClientData/ClientData.cs)：复用回调上下文传递方式。

## 移植与验收重点

- **事件语义**：C# wrapper 继承不会自动建立 native 到托管的虚函数回调。使用 managed `AddObserver`，核对 observer 挂在 interactor 还是 style 上、处理顺序、默认操作与事件中止需求；不能简单重复调用默认事件方法。
- **选择行为**：明确单选、追加、取消、清空和空白点击行为；验证重复选择、未命中、不可拾取 Actor、多视口及遮挡。返回的 wrapper 与场景对象比较时，应按 native 对象身份判断。
- **坐标与容差**：检查屏幕与世界坐标转换、Actor 变换、高 DPI 和窗口缩放；验证极大/极小坐标、边界点及拾取容差。
- **数据与 ID**：关注空数据、退化单元、过滤器重新编号、glyph 输出与输入映射；原始 VTK ID 与业务实体 ID 分别维护。
- **高亮显示**：恢复原属性，检查标量着色与高亮颜色的关系；验证叠加深度冲突、相机变化、取消选择和高亮 Actor 自身是否参与拾取。
- **生命周期**：保活托管委托与关联对象；借用 picker 结果时保持 native 对象有效；`callData` 在回调内读取或复制，不能保留临时指针。重复打开、关闭和销毁示例需验证。
- **拖动编辑**：区分相机操作与数据编辑，检查按下/移动/释放状态、窗口外释放、`Modified()` 和管线更新。工程应用另需定义约束、撤销和网格有效性规则。
- **数据准备**：外部图像和可选模型固定官方源码 revision，按项目流程记录文件来源；优先使用原例提供的自生成输入路径。
- **验收**：按[统一验证流程](../workflow/verification.md)检查绑定生成一致性、构建和目标示例截图；鼠标、键盘、框选及拖动必须实际操作验证，静态截图不能代替交互验收。

具体移植按[示例翻译流程](../workflow/ai-assisted-development.md#10-vtk-示例翻译流程)执行，通过 `plan-bindings` 与候选白名单补充最小 API，生成代码问题在 generator/配置中修复。
