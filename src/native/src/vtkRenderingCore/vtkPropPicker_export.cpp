#include "vtksharp_api.h"
#include <vtkActor.h>
#include <vtkProp.h>
#include <vtkPropPicker.h>
#include <vtkRenderer.h>

VTKSHARP_API vtkPropPicker* vtkPropPicker_New()
{
    return vtkPropPicker::New();
}

VTKSHARP_API int vtkPropPicker_Pick(
    vtkPropPicker* self,
    double selectionX,
    double selectionY,
    double selectionZ,
    vtkRenderer* renderer)
{
    return self->Pick(selectionX, selectionY, selectionZ, renderer);
}

VTKSHARP_API vtkActor* vtkPropPicker_GetActor(vtkPropPicker* self)
{
    return self->GetActor();
}

VTKSHARP_API vtkProp* vtkPropPicker_GetViewProp(vtkPropPicker* self)
{
    return self->GetViewProp();
}
