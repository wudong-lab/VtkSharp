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

VTKSHARP_API void vtkGenericRenderWindowInteractor_MouseMoveEvent(vtkGenericRenderWindowInteractor* self)
{
    self->MouseMoveEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_LeftButtonPressEvent(vtkGenericRenderWindowInteractor* self)
{
    self->LeftButtonPressEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_LeftButtonReleaseEvent(vtkGenericRenderWindowInteractor* self)
{
    self->LeftButtonReleaseEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_MiddleButtonPressEvent(vtkGenericRenderWindowInteractor* self)
{
    self->MiddleButtonPressEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_MiddleButtonReleaseEvent(vtkGenericRenderWindowInteractor* self)
{
    self->MiddleButtonReleaseEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_RightButtonPressEvent(vtkGenericRenderWindowInteractor* self)
{
    self->RightButtonPressEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_RightButtonReleaseEvent(vtkGenericRenderWindowInteractor* self)
{
    self->RightButtonReleaseEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_MouseWheelForwardEvent(vtkGenericRenderWindowInteractor* self)
{
    self->MouseWheelForwardEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_MouseWheelBackwardEvent(vtkGenericRenderWindowInteractor* self)
{
    self->MouseWheelBackwardEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_EnterEvent(vtkGenericRenderWindowInteractor* self)
{
    self->EnterEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_LeaveEvent(vtkGenericRenderWindowInteractor* self)
{
    self->LeaveEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_KeyPressEvent(vtkGenericRenderWindowInteractor* self)
{
    self->KeyPressEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_KeyReleaseEvent(vtkGenericRenderWindowInteractor* self)
{
    self->KeyReleaseEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_CharEvent(vtkGenericRenderWindowInteractor* self)
{
    self->CharEvent();
}

VTKSHARP_API void vtkGenericRenderWindowInteractor_TimerEventResetsTimerOff(
    vtkGenericRenderWindowInteractor* self)
{
    self->TimerEventResetsTimerOff();
}
