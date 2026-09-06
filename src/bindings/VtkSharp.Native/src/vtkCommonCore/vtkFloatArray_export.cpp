#include "vtksharp_api.h"
#include <vtkFloatArray.h>

VTKSHARP_API float* vtkFloatArray_GetPointer(vtkFloatArray* self, vtkIdType valueIdx)
{
    return self->GetPointer(valueIdx);
}
