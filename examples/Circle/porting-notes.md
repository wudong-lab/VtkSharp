# Circle — VTK Circle Example Porting Notes

**Original**: `VTK/Examples/GeometricObjects/Cxx/Circle.cxx`
**Date**: 2026-06-25
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkRegularPolygonSource | vtkFiltersSources | ✅ newly whitelisted |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted |
| vtkActor | vtkRenderingCore | ✅ already whitelisted |
| vtkProperty | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderer | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindow | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted |
| vtkNamedColors | vtkCommonColor | ✅ already whitelisted (uses fixed-array GetColorRGB) |

## Added API

- vtkRegularPolygonSource::GeneratePolygonOff()
- vtkRegularPolygonSource::SetNumberOfSides(int)
- vtkRegularPolygonSource::SetRadius(double)
- vtkRegularPolygonSource::SetCenter(double, double, double)
- vtkRegularPolygonSource::SetCenter(const double[3])

## Deviations from C++ original

- Uses vtkNamedColors::GetColorRGB(name, Span<double>) instead of GetColor3d(name).GetData() (value-type vtkColor3d not bound; fixed-array overload used instead).
  - Cornsilk via `colors.GetColorRGB("Cornsilk", stackalloc double[3])`
  - DarkGreen via `colors.GetColorRGB("DarkGreen", stackalloc double[3])`
