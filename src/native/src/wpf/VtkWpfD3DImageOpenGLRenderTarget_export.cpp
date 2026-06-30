#include "VtkWpfD3DImageOpenGLRenderTarget.h"
#include "vtksharp_api.h"

VTKSHARP_API VtkWpfD3DImageOpenGLRenderTarget* VtkWpfD3DImageOpenGLRenderTarget_New()
{
    return VtkWpfD3DImageOpenGLRenderTarget::Create();
}

VTKSHARP_API void VtkWpfD3DImageOpenGLRenderTarget_Delete(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    delete target;
}

VTKSHARP_API vtkRenderWindow* VtkWpfD3DImageOpenGLRenderTarget_GetRenderWindow(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetRenderWindow() : nullptr;
}

VTKSHARP_API vtkRenderer* VtkWpfD3DImageOpenGLRenderTarget_GetRenderer(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetRenderer() : nullptr;
}

VTKSHARP_API void VtkWpfD3DImageOpenGLRenderTarget_SetSize(
    VtkWpfD3DImageOpenGLRenderTarget* target,
    int width,
    int height)
{
    if (target)
    {
        target->SetSize(width, height);
    }
}

VTKSHARP_API void VtkWpfD3DImageOpenGLRenderTarget_Render(VtkWpfD3DImageOpenGLRenderTarget* target)
{
    if (target)
    {
        target->Render();
    }
}

VTKSHARP_API IDirect3DSurface9* VtkWpfD3DImageOpenGLRenderTarget_GetBackBuffer(
    VtkWpfD3DImageOpenGLRenderTarget* target)
{
    return target ? target->GetBackBuffer() : nullptr;
}

VTKSHARP_API const char* VtkWpfD3DImageOpenGLRenderTarget_GetLastError()
{
    return VtkWpfD3DImageOpenGLRenderTarget::GetLastError();
}
