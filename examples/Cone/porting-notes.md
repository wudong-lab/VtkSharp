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
