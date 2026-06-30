#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>

#include <d3d9.h>
#include <gl/GL.h>
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
    using GlGenFramebuffersProc = void(APIENTRY*)(GLsizei, GLuint*);
    using GlBindFramebufferProc = void(APIENTRY*)(GLenum, GLuint);
    using GlFramebufferTexture2DProc = void(APIENTRY*)(GLenum, GLenum, GLenum, GLuint, GLint);
    using GlCheckFramebufferStatusProc = GLenum(APIENTRY*)(GLenum);
    using GlDeleteFramebuffersProc = void(APIENTRY*)(GLsizei, const GLuint*);

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
    bool CheckHr(HRESULT hr, const char* message);

    bool LoadOpenGLExtensions();
    bool CreateD3DDevice();
    bool OpenDxInteropDevice();
    bool InitializeVtk();
    bool CreateInteropResource(int width, int height);
    void ReleaseInteropResource();

    vtkSmartPointer<vtkCallbackCommand> CreateCallback(CallbackMethod method);
    static void InvokeCallback(vtkObject* caller, unsigned long eventId, void* clientData, void* callData);
    static void DeleteCallbackState(void* clientData);

    void OnMakeCurrent(void* callData);
    void OnIsCurrent(void* callData);
    void OnSupportsOpenGL(void* callData);
    void OnIsDirect(void* callData);
    void OnFrame(void* callData);

    WglContext m_wglContext;
    WglDxInteropApi m_wglDxInteropApi;

    GlGenFramebuffersProc m_glGenFramebuffers = nullptr;
    GlBindFramebufferProc m_glBindFramebuffer = nullptr;
    GlFramebufferTexture2DProc m_glFramebufferTexture2D = nullptr;
    GlCheckFramebufferStatusProc m_glCheckFramebufferStatus = nullptr;
    GlDeleteFramebuffersProc m_glDeleteFramebuffers = nullptr;

    IDirect3D9Ex* m_direct3D = nullptr;
    IDirect3DDevice9Ex* m_d3DDevice = nullptr;
    IDirect3DTexture9* m_texture = nullptr;
    IDirect3DSurface9* m_surface = nullptr;

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
