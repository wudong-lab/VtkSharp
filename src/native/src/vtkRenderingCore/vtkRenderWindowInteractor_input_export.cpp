#include "vtksharp_api.h"
#include <vtkCommand.h>
#include <vtkRenderWindowInteractor.h>

VTKSHARP_API void vtkRenderWindowInteractor_EnableRenderOff(vtkRenderWindowInteractor* self)
{
    self->EnableRenderOff();
}

VTKSHARP_API void vtkRenderWindowInteractor_EnableRenderOn(vtkRenderWindowInteractor* self)
{
    self->EnableRenderOn();
}

VTKSHARP_API void vtkRenderWindowInteractor_SetAltKey(vtkRenderWindowInteractor* self, int altKey)
{
    self->SetAltKey(altKey);
}

VTKSHARP_API void vtkRenderWindowInteractor_SetEventInformationFlipY(
    vtkRenderWindowInteractor* self,
    int x,
    int y,
    int controlKey,
    int shiftKey,
    char keyCode,
    int repeatCount)
{
    self->SetEventInformationFlipY(x, y, controlKey, shiftKey, keyCode, repeatCount);
}

VTKSHARP_API void vtkRenderWindowInteractor_SetEventInformation(
    vtkRenderWindowInteractor* self,
    int x,
    int y,
    int controlKey,
    int shiftKey,
    char keyCode,
    int repeatCount)
{
    self->SetEventInformation(x, y, controlKey, shiftKey, keyCode, repeatCount);
}

VTKSHARP_API void vtkRenderWindowInteractor_SetKeyEventInformation(
    vtkRenderWindowInteractor* self,
    int controlKey,
    int shiftKey,
    char keyCode,
    int repeatCount)
{
    self->SetKeyEventInformation(controlKey, shiftKey, keyCode, repeatCount);
}

VTKSHARP_API int vtkRenderWindowInteractor_CreateRepeatingTimer(
    vtkRenderWindowInteractor* self,
    int duration)
{
    return self->CreateRepeatingTimer(static_cast<unsigned long>(duration));
}

VTKSHARP_API int vtkRenderWindowInteractor_CreateOneShotTimer(
    vtkRenderWindowInteractor* self,
    int duration)
{
    return self->CreateOneShotTimer(static_cast<unsigned long>(duration));
}

VTKSHARP_API int vtkRenderWindowInteractor_DestroyTimer_int(vtkRenderWindowInteractor* self, int timerId)
{
    return self->DestroyTimer(timerId);
}

VTKSHARP_API int vtkRenderWindowInteractor_GetTimerEventId(vtkRenderWindowInteractor* self)
{
    return self->GetTimerEventId();
}

VTKSHARP_API void vtkRenderWindowInteractor_SetTimerEventId(vtkRenderWindowInteractor* self, int timerId)
{
    self->SetTimerEventId(timerId);
}

VTKSHARP_API int vtkRenderWindowInteractor_GetTimerEventType(vtkRenderWindowInteractor* self)
{
    return self->GetTimerEventType();
}

VTKSHARP_API int vtkRenderWindowInteractor_GetTimerEventDuration(vtkRenderWindowInteractor* self)
{
    return self->GetTimerEventDuration();
}

VTKSHARP_API int vtkRenderWindowInteractor_GetTimerEventPlatformId(vtkRenderWindowInteractor* self)
{
    return self->GetTimerEventPlatformId();
}

VTKSHARP_API void vtkRenderWindowInteractor_SetTimerEventPlatformId(
    vtkRenderWindowInteractor* self,
    int platformTimerId)
{
    self->SetTimerEventPlatformId(platformTimerId);
}

VTKSHARP_API void vtkRenderWindowInteractor_InvokeTimerEvent(vtkRenderWindowInteractor* self, int timerId)
{
    self->InvokeEvent(vtkCommand::TimerEvent, &timerId);
}
