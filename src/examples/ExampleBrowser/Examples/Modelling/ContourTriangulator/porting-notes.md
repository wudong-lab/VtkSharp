# ContourTriangulator — VTK Example Porting Notes

**Original**: `VTK/Examples/Modelling/Cxx/ContourTriangulator.cxx`
**Source URL**: https://examples.vtk.org/site/Cxx/Modelling/ContourTriangulator/
**Date**: 2026-09-20
**Status**: candidate merged, smoke test passed

## Translation summary

- 从 `fullhead15.png` 读取图像，用 `vtkMarchingSquares` 提取 `isoValue = 500` 的等值线，
  再用 `vtkContourTriangulator` 将闭合等值线填充为三角化多边形。
- 紫色 `MediumOrchid` 显示原始等值线，灰色 `Gray` 显示填充结果，背景 `DarkSlateGray`。
- C++ 版本通过命令行接收 PNG 路径和等值线值；C# 版本固定使用示例资源
  `Data/fullhead15.png` 和默认 `isoValue = 500`，与官方无参数默认值一致。
- 相机在 `ResetCamera()` 后执行 `Azimuth(180)`，与原示例一致。
- 实现 `ISmokeExample`，截图路径分支在进入交互事件循环前保存首帧。

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkNamedColors | vtkCommonColor | already whitelisted |
| vtkPNGReader | vtkIOImage | newly whitelisted (CanReadFile) |
| vtkImageReader2 | vtkIOImage | newly whitelisted as base of vtkPNGReader (SetFileName) |
| vtkMarchingSquares | vtkFiltersCore | newly whitelisted (SetValue) |
| vtkContourTriangulator | vtkFiltersGeneral | newly whitelisted (type only) |
| vtkDataSetMapper | vtkRenderingCore | newly whitelisted (type only) |
| vtkActor | vtkRenderingCore | already whitelisted |
| vtkProperty | vtkRenderingCore | already whitelisted (SetColor) |
| vtkRenderer | vtkRenderingCore | already whitelisted |
| vtkCamera | vtkRenderingCore | already whitelisted (Azimuth) |
| vtkRenderWindow | vtkRenderingCore | already whitelisted |
| vtkRenderWindowInteractor | vtkRenderingCore | already whitelisted |
| vtkPNGWriter | vtkIOImage | already whitelisted (smoke screenshot) |

## Added API

由 `plan-bindings` / `diff-whitelist --summary` 生成（5 个类型，3 个函数）：

- **vtkMarchingSquares** (vtkFiltersCore): `SetValue(int, double)`
- **vtkContourTriangulator** (vtkFiltersGeneral): 仅类型
- **vtkImageReader2** (vtkIOImage): `SetFileName(const char*)`，作为 vtkPNGReader 基类加入
- **vtkPNGReader** (vtkIOImage): `CanReadFile(const char*)`
- **vtkDataSetMapper** (vtkRenderingCore): 仅类型

以下调用来自已导出的基类，未重复导出：

- `SetInputConnection` / `GetOutputPort` / `Update`：`vtkAlgorithm`
- `ScalarVisibilityOff`：`vtkMapper`
- `SetMapper` / `GetProperty`：`vtkActor` / `vtkProp`

## Data files

| File | Source | Revision | SHA-256 | Size | Target |
|------|--------|----------|---------|------|--------|
| fullhead15.png | https://gitlab.kitware.com/vtk/vtk-examples/-/raw/master/src/Testing/Data/fullhead15.png | vtk-examples `3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41` | A6D71CEA50D99A477A22A562717E7900D8A22208A77B20E86F85F1E86FADD192 | 67675 | `Examples/Modelling/ContourTriangulator/Data/fullhead15.png` |

无附属文件；数据通过 `ExampleBrowser.csproj` 的 `Content`/`CopyToOutputDirectory` 复制到输出目录 `Data/`。

## Deviations from C++ original

- 命令行参数改为固定资源路径与常量 `isoValue = 500`；`CanReadFile` 失败时用
  `Debug.WriteLine` 记录并提前返回，等价于原示例的错误分支。
- `vtkNamedColors::GetColor3d(...).GetData()` 使用 VtkSharp 的 `VtkColor3d` 重载
  （`.R`/`.G`/`.B`），功能等价。
- 截图分支使用 `vtkRenderWindow.GetRgbImageData()` + `vtkPNGWriter`，属于示例验收扩展，
  不影响交互路径。

## Verification

- 统一验证报告：`artifacts/verification/20260920-140315-37760263/verification.json`
  - generator-build / generator-tests / generate / native-build / managed-tests /
    example-build / example-smoke / generated-check 全部 passed。
- 首帧截图：`artifacts/verification/20260920-140315-37760263/example/screenshot.png`
  - 300×300，深灰背景上可见灰色三角化头部轮廓与紫色等值线，与官方示例预期一致。
- 仍需人工确认：交互（鼠标旋转/缩放/关闭）、重复创建-释放，未自动化。
