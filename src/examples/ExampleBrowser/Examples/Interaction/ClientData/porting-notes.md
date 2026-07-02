# ClientData — VTK ClientData Example Porting Notes

**Original**: `https://gitlab.kitware.com/vtk/vtk-examples/-/blob/master/src/Cxx/Interaction/ClientData.cxx`
**Date**: 2026-06-27
**Status**: candidate merged

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkSphereSource | vtkFiltersSources | ✅ whitelisted (added SetCenter/GetRadius) |
| vtkPolyDataMapper | vtkRenderingCore | ✅ whitelisted |
| vtkActor | vtkRenderingCore | ✅ whitelisted |
| vtkProperty | vtkRenderingCore | ✅ whitelisted |
| vtkNamedColors | vtkCommonColor | ✅ whitelisted |
| vtkRenderer | vtkRenderingCore | ✅ whitelisted |
| vtkRenderWindow | vtkRenderingCore | ✅ whitelisted |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ whitelisted |

## Added API

| Method | Module | Class |
|--------|--------|-------|
| `SetCenter(double, double, double)` | vtkFiltersSources | vtkSphereSource |
| `SetCenter(const double[3])` | vtkFiltersSources | vtkSphereSource |
| `GetRadius()` | vtkFiltersSources | vtkSphereSource |

## Deviations from C++ Original

### vtkCallbackCommand replaced with managed AddObserver

The C++ original uses `vtkCallbackCommand` with `SetClientData()` and `SetCallback()` to pass the `vtkSphereSource` as client data to a static callback function. VtkSharp does not expose `vtkCallbackCommand` directly — instead, it provides a managed `AddObserver` overload that accepts a `clientData` parameter (`object?`).

The C# port uses:
```csharp
renderWindowInteractor.AddObserver(
    vtkCommand.KeyPressEvent,
    KeypressCallbackFunction,
    clientData: sphereSource);
```

Where `KeypressCallbackFunction` has the managed delegate signature:
```csharp
void (vtkObject caller, uint eventId, object? clientData, nint callData)
```

The `clientData` object is cast back to `vtkSphereSource` inside the callback, equivalent to the C++ `static_cast<vtkSphereSource*>(clientData)`.

## Unsupported / Skipped

None — all types used were already supported.
