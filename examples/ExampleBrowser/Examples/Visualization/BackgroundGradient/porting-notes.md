# BackgroundGradient — VTK BackgroundGradient Example Porting Notes

**Original**: `VTKExamples/src/Cxx/Visualization/BackgroundGradient.cxx`
**Date**: 2026-06-26
**Status**: candidate merged

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkSphereSource | vtkFiltersSources | ✅ already whitelisted |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted |
| vtkActor | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderer | vtkRenderingCore | ✅ whitelisted (added GradientBackgroundOn, SetBackground2) |
| vtkRenderWindow | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted |
| vtkNamedColors | vtkCommonColor | ✅ already whitelisted (GetColor3d) |

## Added API

- vtkRenderer::GradientBackgroundOn() → void
- vtkRenderer::SetBackground2(double, double, double) → void
- vtkRenderer::SetBackground2(const double[3]) → void

## Unsupported / Skipped

None — all types used were already supported.

## Deviations from C++ Original

- `colors->GetColor3d("Banana").GetData()` → `colors.GetColor3d("Banana")` with `.R/.G/.B` property access. The C# binding returns a `VtkColor3d` value type struct instead of a pointer, so `.GetData()` is unnecessary.
