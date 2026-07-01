#include "vtksharp_api.h"
#include <vtkCommand.h>
#include <vtkRenderWindowInteractor.h>

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

VTKSHARP_API void vtkRenderWindowInteractor_InvokeTimerEvent(vtkRenderWindowInteractor* self, int timerId)
{
    self->InvokeEvent(vtkCommand::TimerEvent, &timerId);
}
