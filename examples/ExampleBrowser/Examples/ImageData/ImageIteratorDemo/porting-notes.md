# ImageIteratorDemo — VTK ImageIteratorDemo Example Porting Notes

**Original**: `https://examples.vtk.org/site/Cxx/ImageData/ImageIteratorDemo/`
**Date**: 2026-07-02
**Status**: candidate merged

## VTK Classes Used

| Class | Module | Status |
|-------|--------|--------|
| vtkImageData | vtkCommonDataModel | ✅ whitelisted (added AllocateScalars, SetScalarComponentFromDouble) |
| vtkCartesianGrid | vtkCommonDataModel | ✅ whitelisted (new class, added SetDimensions) |
| vtkImageViewer2 | vtkInteractionImage | ✅ whitelisted (new class) |
| vtkInteractorStyleImage | vtkInteractionStyle | ✅ whitelisted (new class) |
| vtkRenderWindowInteractor | vtkRenderingCore | ✅ whitelisted (already had SetInteractorStyle, Start) |
| vtkImageActor | vtkRenderingCore | ✅ whitelisted (added InterpolateOff) |
| vtkNamedColors | vtkCommonColor | ✅ whitelisted |
| vtkRenderWindow | vtkRenderingCore | ✅ whitelisted (SetWindowName on base vtkWindow) |
| vtkRenderer | vtkRenderingCore | ✅ whitelisted (SetBackground on base vtkViewport) |

## Added API

| Module | Class | Method |
|--------|-------|--------|
| vtkCommonDataModel | vtkCartesianGrid | SetDimensions(int, int, int) |
| vtkCommonDataModel | vtkCartesianGrid | SetDimensions(const int[3]) |
| vtkCommonDataModel | vtkImageData | AllocateScalars(int, int) |
| vtkCommonDataModel | vtkImageData | AllocateScalars(vtkInformation*) |
| vtkCommonDataModel | vtkImageData | SetScalarComponentFromDouble(int, int, int, int, double) |
| vtkInteractionImage | vtkImageViewer2 | Render() |
| vtkInteractionImage | vtkImageViewer2 | SetInputData(vtkImageData*) |
| vtkInteractionImage | vtkImageViewer2 | SetSlice(int) |
| vtkInteractionImage | vtkImageViewer2 | GetRenderWindow() |
| vtkInteractionImage | vtkImageViewer2 | GetRenderer() |
| vtkInteractionImage | vtkImageViewer2 | GetImageActor() |
| vtkInteractionImage | vtkImageViewer2 | SetupInteractor(vtkRenderWindowInteractor*) |
| vtkInteractionStyle | vtkInteractorStyleImage | SetInteractionModeToImageSlicing() |
| vtkRenderingCore | vtkImageActor | InterpolateOff() |

## Deviations from C++ Original

### vtkImageIterator is a C++ template — cannot be bound
The core class demonstrated by this example, `vtkImageIterator<unsigned char>`, is a C++ template class. Its `BeginSpan()` and `EndSpan()` methods return `SpanIterator` (a template-dependent pointer type). The VtkSharp P/Invoke binding system cannot represent C++ template classes. Instead, the C# port manually iterates the sub-extent with nested `for` loops over x, y, z dimensions, using `SetScalarComponentFromDouble()` to set each component.

### GetScalarPointer returns void* — unsupported
The C++ example uses `imageData->GetScalarPointer(0, 0, 0)` to get a raw unsigned char pointer for direct memory access. The binding generator excludes functions returning `void*` (non-vtkObject pointers). The C# port uses `SetScalarComponentFromDouble(x, y, z, component, value)` per-pixel instead.

### GetDimensions returns int* — unsupported
The C++ example uses `imageData->GetDimensions()` to retrieve the image dimensions as a raw int array. The binding generator excludes functions returning raw pointers. The C# port uses the known constant dimensions (100, 200, 30) directly.

### GetIncrements / GetContinuousIncrements — unsupported
The C++ example prints increment values using `GetIncrements()` (returns `vtkIdType*`) and `GetContinuousIncrements(int[6], vtkIdType&, vtkIdType&, vtkIdType&)` (uses reference parameters). Both are excluded by the binding generator. The C# port prints the number of z-slices modified and total pixels modified instead.

### Color value scaling
The C++ example uses `vtkNamedColors::GetColor(name, unsigned char&, ...)` which returns 0–255 integer values suitable for `VTK_UNSIGNED_CHAR` pixel data. VtkSharp binds `GetColor(name, double[4])` which returns 0.0–1.0 normalized values. However, `SetScalarComponentFromDouble` handles the conversion to unsigned char correctly, so no manual scaling is needed.
