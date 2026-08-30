# Cone — VTK Cone Example Porting Notes

**Original**: `VTK/Examples/GeometricObjects/Cxx/Cone.cxx`
**Date**: 2026-06-25
**Status**: candidate merged

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkConeSource | vtkFiltersSources | ✅ whitelisted (SetCenter before, added SetHeight/SetRadius/SetResolution) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ whitelisted (SetInputConnection) |
| vtkActor | vtkRenderingCore | ✅ whitelisted (SetMapper) |
| vtkRenderer | vtkRenderingCore | ✅ whitelisted (AddActor) |
| vtkRenderWindow | vtkRenderingCore | ✅ whitelisted (AddRenderer) |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ whitelisted (SetRenderWindow, Start) |

## Added API

- vtkConeSource::SetHeight(double)
- vtkConeSource::SetRadius(double)
- vtkConeSource::SetResolution(int)

## Unsupported / Skipped

None — all types used were already supported.

## 自动验收（2026-08-30）

Cone 实现可选 `ISmokeExample`，交互与截图模式共用场景构造。截图模式在渲染后保存 PNG 并返回，不进入交互循环；原有 `Run()` 行为保持不变。

通过 `tools/verify-workflow.ps1 -VtkDir <vtk-cmake-directory> -Example GeometricObjects/Cone` 运行。此次输出 800×600 PNG，解码及退出成功，人工查看锥体正常；未验证长时间交互和重复创建/销毁。报告位于 `artifacts/verification/20260830-190313-0f961f37/verification.json`。

截图复用已有 API，没有新增绑定。图像跨 filter 生命周期使用的依据见 [截图数据持有关系](../../../../../../docs/interop/window-image.md)。

## 首页展示

渲染背景使用 `VtkColor3d.LightSkyBlue`（`#87CEFA`）。

首页代码与本示例统一使用高度 3、半径 1、分辨率 32 的品红色圆锥（`VtkColor3d.Magenta`），并通过 `vtkTextActor`
在窗口左上角显示 `VtkSharp - open-source .NET binding for VTK`。文字为黑色，字号 28，
使用归一化视口坐标 `(0.025, 0.95)` 和顶部对齐，窗口缩放时仍保持在左上区域；
800×600 时左边距为 20、上边距为 30 像素。复用现有绑定，没有新增 API。

Release 构建与截图验收通过，人工确认圆锥颜色和完整文字正常。原始 800×600 PNG 保存于
`docs/images/cone-example.png` 并由根 README 引用，未进行图片后处理。
重新生成时，从仓库根目录执行以下命令，输出目录必须尚不存在：

```powershell
dotnet run --project src/examples/ExampleBrowser/ExampleBrowser.csproj --configuration Release -- --smoke GeometricObjects/Cone --output artifacts/cone-readme
```

检查新截图后再更新文档图片；截图验收不替代交互及重复创建/销毁测试。
