# PolyLine — VTK PolyLine Example Porting Notes

**Original**: https://examples.vtk.org/site/Cxx/GeometricObjects/PolyLine/
**Date**: 2026-06-25
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkPoints | vtkCommonCore | ✅ newly whitelisted |
| vtkIdList | vtkCommonCore | ✅ newly whitelisted |
| vtkCell | vtkCommonDataModel | ✅ newly whitelisted (abstract base) |
| vtkPolyLine | vtkCommonDataModel | ✅ newly whitelisted |
| vtkCellArray | vtkCommonDataModel | ✅ newly whitelisted |
| vtkPolyData | vtkCommonDataModel | ✅ newly whitelisted |
| vtkPolyDataMapper | vtkRenderingCore | ✅ extended |
| vtkActor | vtkRenderingCore | ✅ already whitelisted |
| vtkProperty | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderer | vtkRenderingCore | ✅ already whitelisted |
| vtkRenderWindow | vtkRenderingCore | ✅ extended |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ already whitelisted |
| vtkNamedColors | vtkCommonColor | ✅ already whitelisted |

## Added API

- **vtkCommonCore**
  - `vtkPoints::InsertNextPoint(double x, double y, double z) -> vtkIdType`
  - `vtkIdList::SetNumberOfIds(vtkIdType)`
  - `vtkIdList::SetId(vtkIdType i, vtkIdType vtkid)`
- **vtkCommonDataModel**
  - `vtkCell::GetPointIds() -> vtkIdList*` (inherited by vtkPolyLine)
  - `vtkCellArray::InsertNextCell(vtkCell* cell) -> vtkIdType`
  - `vtkPolyData::SetPoints(vtkPoints*)`
  - `vtkPolyData::SetLines(vtkCellArray*)`
- **vtkRenderingCore**
  - `vtkPolyDataMapper::SetInputData(vtkPolyData*)`
  - `vtkRenderWindow::Render()`

## Generator Fix

- Fixed `CSharpBindingEmitter` to escape C# reserved keywords in parameter names (e.g., VTK parameter named `in` → emitted as `@in`). The parameter `in` from `SetInputData(vtkPolyData* in)` previously caused a C# compilation error.

## Deviations from C++ original

- **No `double[3]` stack arrays**: C++ code declares `double origin[3] = {0,0,0}` and passes arrays to `InsertNextPoint(origin)`. The C# port uses the scalar overload `InsertNextPoint(x, y, z)` instead.
- **`GetPointIds()` not wrapped in `using`**: `polyLine->GetPointIds()` returns a non-owned `vtkIdList*` (borrowed reference); it must not be disposed by the caller. The returned object is used directly without a `using` statement.
- **Color access via `VtkColor3d` struct**: C++ code uses `colors->GetColor3d("Tomato").GetData()` (returns `const double*`). C# uses the `VtkColor3d` value type returned by `GetColor3d(name)` and accesses `.R/.G/.B` properties directly.
- **`EXIT_SUCCESS` → implicit return**: C# `Main()` has `void` return type; no explicit return value needed.
