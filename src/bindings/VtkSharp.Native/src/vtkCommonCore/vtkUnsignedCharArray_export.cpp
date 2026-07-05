#include "vtksharp_api.h"
#include <vtkUnsignedCharArray.h>

VTKSHARP_API void vtkUnsignedCharArray_SetUnsignedCharTuple(vtkUnsignedCharArray* self, vtkIdType tupleIdx, const unsigned char* tuple) { self->SetTypedTuple(tupleIdx, tuple); }
