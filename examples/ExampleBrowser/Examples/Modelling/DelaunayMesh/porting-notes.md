# DelaunayMesh — VTK Delaunay Triangulation Example Porting Notes

**Original**: `VTK/Examples/Modelling/Cxx/DelaunayMesh.cxx`
**Date**: 2026-06-26
**Status**: candidate merged, smoke test passed

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkNamedColors | vtkCommonColor | ✅ already whitelisted |
| vtkMinimalStandardRandomSequence | vtkCommonCore | ✅ newly whitelisted (SetSeed, GetValue, Next) |
| vtkPoints | vtkCommonCore | ✅ added InsertPoint; InsertNextPoint already available |
| vtkPolyData | vtkCommonDataModel | ✅ already whitelisted (SetPoints) |
| vtkDelaunay2D | vtkFiltersCore | ✅ newly whitelisted (SetInputData, SetTolerance, GetOutputPort) |
| vtkExtractEdges | vtkFiltersCore | ✅ newly whitelisted (SetInputConnection, GetOutputPort) |
| vtkGlyph3D | vtkFiltersCore | ✅ newly whitelisted (SetInputConnection, SetSourceConnection, GetOutputPort) |
| vtkTubeFilter | vtkFiltersCore | ✅ newly whitelisted (SetRadius, SetNumberOfSides, SetInputConnection, GetOutputPort) |
| vtkSphereSource | vtkFiltersSources | ✅ newly whitelisted (SetRadius, SetThetaResolution, SetPhiResolution, GetOutputPort) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ already whitelisted |
| vtkActor | vtkRenderingCore | ✅ already whitelisted (SetMapper, GetProperty) |
| vtkProperty | vtkRenderingCore | ✅ added SetAmbient, SetDiffuse, SetSpecular, SetSpecularColor, SetSpecularPower; SetColor already available |
| vtkRenderer | vtkRenderingCore | ✅ already whitelisted (AddActor, ResetCamera, GetActiveCamera, SetBackground via vtkViewport) |
| vtkCamera | vtkRenderingCore | ✅ already whitelisted (Zoom) |
| vtkRenderWindow | vtkRenderingCore | ✅ already whitelisted (AddRenderer, Render, SetWindowName, SetSize) |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ added Initialize; SetRenderWindow, Start already available |

## Added API

### New Classes (6)

- **vtkMinimalStandardRandomSequence** (vtkCommonCore): SetSeed(int), GetValue()→double, Next()
- **vtkDelaunay2D** (vtkFiltersCore): SetInputData(vtkDataObject*), SetInputData(int, vtkDataObject*), SetTolerance(double), GetOutputPort(), GetOutputPort(int)
- **vtkExtractEdges** (vtkFiltersCore): SetInputConnection(vtkAlgorithmOutput*), SetInputConnection(int, vtkAlgorithmOutput*), GetOutputPort(), GetOutputPort(int)
- **vtkGlyph3D** (vtkFiltersCore): SetInputConnection(vtkAlgorithmOutput*), SetInputConnection(int, vtkAlgorithmOutput*), SetSourceConnection(vtkAlgorithmOutput*), SetSourceConnection(int, vtkAlgorithmOutput*), GetOutputPort(), GetOutputPort(int)
- **vtkTubeFilter** (vtkFiltersCore): SetRadius(double), SetNumberOfSides(int), SetInputConnection(vtkAlgorithmOutput*), SetInputConnection(int, vtkAlgorithmOutput*), GetOutputPort(), GetOutputPort(int)
- **vtkSphereSource** (vtkFiltersSources): SetRadius(double), SetThetaResolution(int), SetPhiResolution(int), GetOutputPort(), GetOutputPort(int)

### Additional Methods on Existing Classes (3)

- **vtkPoints::InsertPoint**(vtkIdType, double, double, double)
- **vtkProperty**: SetAmbient(double), SetDiffuse(double), SetSpecular(double), SetSpecularColor(double, double, double), SetSpecularPower(double)
- **vtkRenderWindowInteractor::Initialize**()

## Deviations from C++ original

- vtkNamedColors color lookups use `GetColor3d()` which returns `VtkSharpColor3d` with `.R`/`.G`/`.B` properties, instead of the C++ `.GetData()` pattern that passes `double*` to `SetColor`. Functionally equivalent.
- vtkIdType renders as C# `long` (int64), which is compatible with the C++ `vtkIdType`.
