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
#include <vtkOpenGLRenderWindow.h>
#include <vtkSmartPointer.h>

class vtkCallbackCommand;
class vtkGenericOpenGLRenderWindow;
class vtkObject;
class vtkRenderWindow;
class vtkRenderer;

class VtkWpfD3DImageOpenGLRenderTarget
{
public:
    static VtkWpfD3DImageOpenGLRenderTarget* Create();
    static const char* GetLastError();

    ~VtkWpfD3DImageOpenGLRenderTarget();

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

    using WglDxSetResourceShareHandleNvProc = BOOL(WINAPI*)(void*, HANDLE);
    using WglDxOpenDeviceNvProc = HANDLE(WINAPI*)(void*);
    using WglDxCloseDeviceNvProc = BOOL(WINAPI*)(HANDLE);
    using WglDxRegisterObjectNvProc = HANDLE(WINAPI*)(HANDLE, void*, GLuint, GLenum, GLenum);
    using WglDxUnregisterObjectNvProc = BOOL(WINAPI*)(HANDLE, HANDLE);
    using WglDxLockObjectsNvProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);
    using WglDxUnlockObjectsNvProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);

    struct WglDxInteropApi
    {
        WglDxSetResourceShareHandleNvProc m_setResourceShareHandle = nullptr;
        WglDxOpenDeviceNvProc m_openDevice = nullptr;
        WglDxCloseDeviceNvProc m_closeDevice = nullptr;
        WglDxRegisterObjectNvProc m_registerObject = nullptr;
        WglDxUnregisterObjectNvProc m_unregisterObject = nullptr;
        WglDxLockObjectsNvProc m_lockObjects = nullptr;
        WglDxUnlockObjectsNvProc m_unlockObjects = nullptr;

        bool Load();
        bool IsAvailable() const;
    };

    class WglContext
    {
    public:
        bool CreateHiddenWindowContext();
        void Release();

        void MakeCurrent() const;
        bool IsCurrent() const;
        vtkOpenGLRenderWindow::VTKOpenGLAPIProc LoadSymbol(const char* name) const;

        HDC DeviceContext() const { return this->m_deviceContext; }

    private:
        HWND m_window = nullptr;
        HDC m_deviceContext = nullptr;
        HGLRC m_glContext = nullptr;
        HMODULE m_openGL32Library = nullptr;
    };

    using CallbackMethod = void (VtkWpfD3DImageOpenGLRenderTarget::*)(void*);

    struct CallbackState
    {
        VtkWpfD3DImageOpenGLRenderTarget* Target;
        CallbackMethod Method;
    };

    VtkWpfD3DImageOpenGLRenderTarget() = default;

    VtkWpfD3DImageOpenGLRenderTarget(const VtkWpfD3DImageOpenGLRenderTarget&) = delete;
    VtkWpfD3DImageOpenGLRenderTarget& operator=(const VtkWpfD3DImageOpenGLRenderTarget&) = delete;

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
