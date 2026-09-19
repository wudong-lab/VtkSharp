# BoundaryEdges 移植记录

- 官方源码：<https://examples.vtk.org/site/Cxx/Meshes/BoundaryEdges/>
- 原始文件：`src/Cxx/Meshes/BoundaryEdges.cxx`
- 移植目标：用 `vtkDiskSource` 生成圆环面，用 `vtkFeatureEdges` 提取边界边（红），叠加显示灰色表面。

## 使用的 VTK 类型

`vtkDiskSource`、`vtkFeatureEdges`、`vtkPolyDataMapper`、`vtkActor`、`vtkProperty`、
`vtkNamedColors`、`vtkRenderer`、`vtkRenderWindow` 和 `vtkRenderWindowInteractor`。

## 新增绑定

候选与规划报告：`artifacts/boundary-edges/candidate.yml`、`artifacts/boundary-edges/report.json`。

- 新类型：`vtkDiskSource`（`vtkFiltersSources`，仅类型）、`vtkFeatureEdges`（`vtkFiltersCore`）。
- 既有类型补充：
  - `vtkFeatureEdges`：`BoundaryEdgesOn`、`FeatureEdgesOff`、`ManifoldEdgesOff`、
    `NonManifoldEdgesOff`、`ColoringOn`（由 VTK 布尔宏展开）。
  - `vtkMapper`：`SetScalarModeToUseCellData`。请求该便捷方法时生成器按枚举组契约
    补齐 `ScalarMode` 枚举、`GetScalarMode`/`SetScalarMode` 及其余便捷方法。
- 未新增模块：`vtkFiltersCore`、`vtkFiltersSources`、`vtkRenderingCore` 均已在既有模块集合中。

## 与官方源码的差异

- 官方 `vtkNamedColors::GetColor3d("Gray").GetData()` 在 C# 侧返回 `VtkColor3d`，
  直接传入已有的 `vtkProperty.SetColor(VtkColor3d)` 与 `vtkViewport.SetBackground(VtkColor3d)` 重载。
- 实现可选 `ISmokeExample`：交互与截图模式共用场景构造，截图模式渲染后保存 PNG 并返回，
  不进入交互循环；`Run()` 行为与官方一致。
- 官方 `printf` 无输出；改用 `Debug.WriteLine` 提示关闭窗口。

## 验证

- `plan-bindings` 全部 `ready`；`diff-whitelist --summary` 新增 2 类 13 函数、0 冲突；
  `merge-candidate` 通过。
- 统一验证脚本 `tools/verify-workflow.ps1 -VtkDir <vtk-cmake-directory> -Regenerate -Example Meshes/BoundaryEdges`
  全部阶段通过（generator 构建/测试、生成、native 构建、managed 测试、示例构建、
  示例 smoke、生成一致性检查）。报告：
  `artifacts/verification/20260919-205150-8d4af5ac/verification.json`。
- 示例 smoke 输出 300×300 PNG，解码及退出成功。人工查看截图：灰色圆环表面，
  内、外两条边界边为红色，背景 DimGray，与官方预期一致。

## 未自动验证项

- 交互（旋转/缩放）未做长时间验证。
- 重复创建/销毁未单独验证。
