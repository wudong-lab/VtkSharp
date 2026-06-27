#include "vtksharp_api.h"
#include <vtkRenderWindowInteractor.h>

VTKSHARP_API void vtkRenderWindowInteractor_GetEventPosition(vtkRenderWindowInteractor* self, int* position)
{
    auto eventPosition = self->GetEventPosition();
    position[0] = eventPosition[0];
    position[1] = eventPosition[1];
}

VTKSHARP_API void vtkRenderWindowInteractor_GetLastEventPosition(vtkRenderWindowInteractor* self, int* position)
{
    auto eventPosition = self->GetLastEventPosition();
    position[0] = eventPosition[0];
    position[1] = eventPosition[1];
}

VTKSHARP_API int vtkRenderWindowInteractor_GetControlKey(vtkRenderWindowInteractor* self)
{
    return self->GetControlKey();
}

VTKSHARP_API int vtkRenderWindowInteractor_GetShiftKey(vtkRenderWindowInteractor* self)
{
    return self->GetShiftKey();
}

VTKSHARP_API int vtkRenderWindowInteractor_GetAltKey(vtkRenderWindowInteractor* self)
{
    return self->GetAltKey();
}

VTKSHARP_API unsigned char vtkRenderWindowInteractor_GetKeyCode(vtkRenderWindowInteractor* self)
{
    return static_cast<unsigned char>(self->GetKeyCode());
}

VTKSHARP_API const char* vtkRenderWindowInteractor_GetKeySym(vtkRenderWindowInteractor* self)
{
    return self->GetKeySym();
}

VTKSHARP_API int vtkRenderWindowInteractor_GetRepeatCount(vtkRenderWindowInteractor* self)
{
    return self->GetRepeatCount();
}
