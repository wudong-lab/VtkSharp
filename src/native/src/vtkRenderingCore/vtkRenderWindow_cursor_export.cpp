#include "vtksharp_api.h"
#include <vtkRenderWindow.h>

VTKSHARP_API int vtkRenderWindow_GetCurrentCursor(vtkRenderWindow* self)
{
    return self->GetCurrentCursor();
}

VTKSHARP_API void vtkRenderWindow_SetCurrentCursor(vtkRenderWindow* self, int cursor)
{
    self->SetCurrentCursor(cursor);
}
