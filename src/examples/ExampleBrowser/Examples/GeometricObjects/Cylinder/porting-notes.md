# Cylinder — VTK Cylinder Example Porting Notes

**Original**: `VTK/Examples/GeometricObjects/Cxx/CylinderExample.cxx`
**Date**: 2026-06-25
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkCylinderSource | vtkFiltersSources | ✅ newly whitelisted (SetResolution) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted |
| vtkActor (→ vtkProp3D) | vtkRenderingCore | ✅ added GetProperty, inherited RotateX/RotateY from vtkProp3D |
| vtkProperty | vtkRenderingCore | ✅ newly whitelisted (SetColor) |
| vtkRenderer | vtkRenderingCore | ✅ added ResetCamera, GetActiveCamera; SetBackground via vtkViewport base |
| vtkCamera | vtkRenderingCore | ✅ newly whitelisted (Zoom) |
| vtkRenderWindow | vtkRenderingCore | ✅ added SetWindowName; AddRenderer/SetSize/Render already available |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted |

## Added API

- vtkCylinderSource::SetResolution(int)
- vtkActor::GetProperty() → vtkProperty*
- vtkProperty::SetColor(double, double, double)
- vtkViewport::SetBackground(double, double, double) — inherited by vtkRenderer
- vtkProp3D::RotateX(double), RotateY(double) — inherited by vtkActor
- vtkRenderer::ResetCamera(), GetActiveCamera() → vtkCamera*
- vtkCamera::Zoom(double)
- vtkRenderWindow::SetWindowName(const char*)

## Deviations from C++ original

- At the time this example was ported, the color values were inlined directly:
  - Tomato = (1.0, 0.388, 0.278)
  - BkgColor = (26/255 ≈ 0.102, 51/255 = 0.2, 102/255 = 0.4)
- Current VtkSharp also supports `vtkNamedColors.GetColor3d(name)` and `VtkColor3d` presets for named-color access.
- Window title "Cylinder" set via SetWindowName.
