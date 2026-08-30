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
