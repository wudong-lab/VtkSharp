#include "VtkOpenGlD3DImageRender.h"
#include "vtksharp_api.h"

VTKSHARP_API VtkOpenGlD3DImageRender* VtkOpenGlD3DImageRender_New()
{
    return VtkOpenGlD3DImageRender::Create();
}

VTKSHARP_API void VtkOpenGlD3DImageRender_Delete(VtkOpenGlD3DImageRender* self)
{
    delete self;
}

VTKSHARP_API vtkRenderWindow* VtkOpenGlD3DImageRender_GetRenderWindow(VtkOpenGlD3DImageRender* self)
{
    return self ? self->GetRenderWindow() : nullptr;
}

VTKSHARP_API vtkRenderer* VtkOpenGlD3DImageRender_GetRenderer(VtkOpenGlD3DImageRender* self)
{
    return self ? self->GetRenderer() : nullptr;
}

VTKSHARP_API void VtkOpenGlD3DImageRender_SetSize(VtkOpenGlD3DImageRender* self, int width, int height)
{
    if (self)
    {
        self->SetSize(width, height);
    }
}

VTKSHARP_API void VtkOpenGlD3DImageRender_Render(VtkOpenGlD3DImageRender* self)
{
    if (self)
    {
        self->Render();
    }
}

VTKSHARP_API IDirect3DSurface9* VtkOpenGlD3DImageRender_GetBackBuffer(VtkOpenGlD3DImageRender* self)
{
    return self ? self->GetBackBuffer() : nullptr;
}

VTKSHARP_API const char* VtkOpenGlD3DImageRender_GetLastError()
{
    return VtkOpenGlD3DImageRender::GetLastError();
}
