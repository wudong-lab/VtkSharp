#include <vtkObjectBase.h>
#include <vtkPoints.h>

extern "C" __declspec(dllexport) vtkPoints* VtkSharpCommonCore_NewPoints()
{
    return vtkPoints::New();
}

extern "C" __declspec(dllexport) const char* VtkSharpCommonCore_GetClassName(vtkObjectBase* object)
{
    return object->GetClassName();
}

extern "C" __declspec(dllexport) void VtkSharpCommonCore_Delete(vtkObjectBase* object)
{
    object->Delete();
}
