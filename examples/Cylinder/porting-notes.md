# Cylinder — VTK Cylinder Example Porting Notes

**Original**: `VTK/Examples/GeometricObjects/Cxx/CylinderExample.cxx`
**Date**: 2026-06-25
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkCylinderSource | vtkFiltersSources | ✅ newly whitelisted (SetResolution) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted (SetInputConnection) |
| vtkActor | vtkRenderingCore | ✅ added GetProperty() → vtkProperty* |
| vtkProperty | vtkRenderingCore | ✅ newly whitelisted (SetColor) |
| vtkRenderer → vtkViewport | vtkRenderingCore | ✅ added SetBackground on vtkViewport |
| vtkRenderWindow | vtkRenderingCore | ✅ already whitelisted (AddRenderer; SetSize/Render via vtkWindow base) |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted (SetRenderWindow, Start) |

## Added API

- vtkCylinderSource::SetResolution(int)
- vtkActor::GetProperty() → vtkProperty*
- vtkProperty::SetColor(double, double, double)
- vtkViewport::SetBackground(double, double, double) — inherited by vtkRenderer

## Notes

- SetBackground is declared on vtkViewport (vtkRenderer's base), so it was added there.
- vtkProperty is a new class; only SetColor is whitelisted for this example.
- The C++ example uses tomato color (1.0, 0.388, 0.278) and dark blue background (0.1, 0.2, 0.4).
