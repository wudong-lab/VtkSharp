#include "vtksharp_api.h"
#include <vtkCallbackCommand.h>
#include <vtkObject.h>

VTKSHARP_API void vtkObject_Modified(vtkObject* self) { self->Modified(); }

VTKSHARP_API unsigned long vtkObject_AddObserver(vtkObject* self, unsigned long eventId, vtkCommand* command, float priority) { return self->AddObserver(eventId, command, priority); }
VTKSHARP_API void vtkObject_RemoveObserver(vtkObject* self, vtkCommand* command) { self->RemoveObserver(command); }
