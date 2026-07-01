#include "vtksharp_api.h"
#include <vtkCommand.h>
#include <vtkRenderWindowInteractor.h>

VTKSHARP_API void vtkRenderWindowInteractor_InvokeTimerEvent(vtkRenderWindowInteractor* self, int timerId)
{
    self->InvokeEvent(vtkCommand::TimerEvent, &timerId);
}
