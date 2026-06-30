#include "vtksharp_api.h"
#include <vtkGenericRenderWindowInteractor.h>

VTKSHARP_API vtkGenericRenderWindowInteractor* vtkGenericRenderWindowInteractor_New()
{
    return vtkGenericRenderWindowInteractor::New();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_TimerEvent(vtkGenericRenderWindowInteractor* self)
{
    self->TimerEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_TimerEventResetsTimerOff(
    vtkGenericRenderWindowInteractor* self)
{
    self->TimerEventResetsTimerOff();
}
