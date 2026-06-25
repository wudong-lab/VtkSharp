# Arrow — VTK Arrow Example Porting Notes

**Original**: `VTK/Examples/GeometricObjects/Cxx/Arrow.cxx`
**Date**: 2026-06-25
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkArrowSource | vtkFiltersSources | ✅ newly whitelisted (no methods; Update/GetOutputPort inherited from vtkAlgorithm) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted |
| vtkActor | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderer | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindow | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted |

## Added API

- vtkArrowSource class (vtkFiltersSources) — `New()` factory only; no specific methods whitelisted.
  `Update()` and `GetOutputPort()` are inherited from vtkAlgorithm and were already available.

## Deviations from C++ original

- **vtkNamedColors** not bound; MidnightBlue = (25/255 ≈ 0.098, 25/255 ≈ 0.098, 112/255 ≈ 0.439) inlined as constants.
- The duplicate `renderWindow->SetWindowName("Arrow");` call in the original is omitted (appears to be a copy-paste artifact; it is called once before the duplicate).

## Generator fix

- Fixed `merge-candidate` to persist new classes even when they have zero functions (previously early-returned without writing when `addedCount == 0`, preventing zero-method base/source classes from being added to the whitelist).
