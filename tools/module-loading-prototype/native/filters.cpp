#include <vtkSphereSource.h>

extern "C" __declspec(dllexport) vtkSphereSource* VtkSharpFiltersSources_NewSphereSource()
{
    return vtkSphereSource::New();
}

extern "C" __declspec(dllexport) double VtkSharpFiltersSources_GetRadius(vtkSphereSource* source)
{
    return source->GetRadius();
}

extern "C" __declspec(dllexport) void VtkSharpFiltersSources_SetRadius(vtkSphereSource* source, double radius)
{
    source->SetRadius(radius);
}
