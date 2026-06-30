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
#include "OpenGlFramebufferApi.h"
#include "WglContext.h"
#include "WglDxInteropApi.h"

#include <vtkSmartPointer.h>

class vtkCallbackCommand;
class vtkGenericOpenGLRenderWindow;
class vtkObject;
class vtkRenderWindow;
class vtkRenderer;

class VtkOpenGlD3DImageRender
{
public:
    static VtkOpenGlD3DImageRender* Create();
    static const char* GetLastError();

    ~VtkOpenGlD3DImageRender();

    vtkRenderWindow* GetRenderWindow() const;
    vtkRenderer* GetRenderer() const;
    IDirect3DSurface9* GetBackBuffer() const;

    void SetSize(int width, int height);
    void Render();

private:
    using CallbackMethod = void (VtkOpenGlD3DImageRender::*)(void*);

    struct CallbackState
    {
        VtkOpenGlD3DImageRender* Target;
        CallbackMethod Method;
    };

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

    vtkSmartPointer<vtkCallbackCommand> CreateCallback(CallbackMethod method);
    static void InvokeCallback(vtkObject* caller, unsigned long eventId, void* clientData, void* callData);
    static void DeleteCallbackState(void* clientData);
    static void RenderVtkWindowCallback(void* userData);

    void RenderVtkWindow();
    void OnMakeCurrent(void* callData);
    void OnIsCurrent(void* callData);
    void OnSupportsOpenGL(void* callData);
    void OnIsDirect(void* callData);
    void OnFrame(void* callData);

    WglContext m_wglContext;
    WglDxInteropApi m_wglDxInteropApi;
    OpenGlFramebufferApi m_openGlFramebufferApi;
    D3DImageRenderTarget m_d3DRenderTarget;

    HANDLE m_dxInteropDevice = nullptr;
    HANDLE m_dxInteropObject = nullptr;

    GLuint m_glTexture = 0;
    GLuint m_framebuffer = 0;

    vtkSmartPointer<vtkGenericOpenGLRenderWindow> m_renderWindow;
    vtkSmartPointer<vtkRenderer> m_renderer;
    vtkSmartPointer<vtkCallbackCommand> m_makeCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> m_isCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> m_supportsOpenGLCallback;
    vtkSmartPointer<vtkCallbackCommand> m_isDirectCallback;
    vtkSmartPointer<vtkCallbackCommand> m_frameCallback;

    int m_width = 1;
    int m_height = 1;
};
