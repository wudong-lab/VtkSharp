#include "VtkWpfD3DImageOpenGLRenderTarget.h"
#include "vtksharp_api.h"

VTKSHARP_API VtkWpfD3DImageOpenGLRenderTarget* VtkWpfOpenGLD3DImageRenderBridge_New()
{
    return VtkWpfD3DImageOpenGLRenderTarget::Create();
}

VTKSHARP_API void VtkWpfOpenGLD3DImageRenderBridge_Delete(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    delete target;
}

VTKSHARP_API vtkRenderWindow* VtkWpfOpenGLD3DImageRenderBridge_GetRenderWindow(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetRenderWindow() : nullptr;
}

VTKSHARP_API vtkRenderer* VtkWpfOpenGLD3DImageRenderBridge_GetRenderer(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetRenderer() : nullptr;
}

VTKSHARP_API void VtkWpfOpenGLD3DImageRenderBridge_SetSize(
    VtkWpfD3DImageOpenGLRenderTarget* target,
    int width,
    int height)
{
    if (target)
    {
        target->SetSize(width, height);
    }
}

VTKSHARP_API void VtkWpfOpenGLD3DImageRenderBridge_Render(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    if (target)
    {
        target->Render();
    }
}

VTKSHARP_API IDirect3DSurface9* VtkWpfOpenGLD3DImageRenderBridge_GetBackBuffer(
    VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetBackBuffer() : nullptr;
}

VTKSHARP_API const char* VtkWpfOpenGLD3DImageRenderBridge_GetLastError()
{
    return VtkWpfD3DImageOpenGLRenderTarget::GetLastError();
}
