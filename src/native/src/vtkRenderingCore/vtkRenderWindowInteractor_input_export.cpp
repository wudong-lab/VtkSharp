#include "vtksharp_api.h"
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
