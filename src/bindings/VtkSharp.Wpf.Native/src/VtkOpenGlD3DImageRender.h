#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>

#include <gl/GL.h>
#include "D3DImageRenderTarget.h"
#include "OpenGlFramebuffer.h"
#include "WglContext.h"
#include "WglDxInterop.h"
#include "VtkSharpExternalOpenGlRenderContext.h"

class vtkRenderWindow;
class vtkRenderer;

class VtkOpenGlD3DImageRender
{
public:
    static VtkOpenGlD3DImageRender* Create();
    ~VtkOpenGlD3DImageRender();

    vtkRenderWindow* GetRenderWindow() const;
    vtkRenderer* GetRenderer() const;
    IDirect3DSurface9* GetBackBuffer() const;

    bool SetSize(int width, int height);
    bool Render();

    static const char* GetLastError();

private:
    VtkOpenGlD3DImageRender() = default;

    VtkOpenGlD3DImageRender(const VtkOpenGlD3DImageRender&) = delete;
    VtkOpenGlD3DImageRender& operator=(const VtkOpenGlD3DImageRender&) = delete;

    bool Initialize();
    void Release();

    void SetError(const char* message);

    bool LoadOpenGLExtensions();
    bool OpenDxInteropDevice();
    bool InitializeVtk();
    bool CreateInteropResource(int width, int height);
    void ReleaseInteropResource();

    static void RenderVtkWindowCallback(void* userData);
    static void* LoadOpenGlSymbolCallback(void* userData, const char* name);
    static int MakeCurrentCallback(void* userData);
    static int IsCurrentCallback(void* userData);
    static void FrameCallback(void* userData);

    void RenderVtkWindow();

    WglContext m_wglContext;
    WglDxInterop m_wglDxInterop;
    OpenGlFramebuffer m_openGlFramebuffer;
    D3DImageRenderTarget m_d3DRenderTarget;
    VtkSharpExternalOpenGlRenderContext* m_vtkContext = nullptr;

    int m_width{1};
    int m_height{1};
};
