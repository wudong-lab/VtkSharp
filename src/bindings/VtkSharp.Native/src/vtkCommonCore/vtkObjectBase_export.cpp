#include "vtksharp_api.h"
#include <vtkObjectBase.h>

VTKSHARP_API void vtkObjectBase_Delete(vtkObjectBase *self) { self->Delete(); }

VTKSHARP_API void vtkObjectBase_Register(vtkObjectBase *self) { self->Register(nullptr); }
VTKSHARP_API void vtkObjectBase_UnRegister(vtkObjectBase *self) { self->UnRegister(nullptr); }

VTKSHARP_API int vtkObjectBase_GetReferenceCount(vtkObjectBase *self) { return self->GetReferenceCount(); }
