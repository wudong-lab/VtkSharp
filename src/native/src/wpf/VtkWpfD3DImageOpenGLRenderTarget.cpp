#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include "VtkWpfD3DImageOpenGLRenderTarget.h"

#include <d3d9.h>
#include <gl/GL.h>
#include <vtkCallbackCommand.h>
#include <vtkCommand.h>
#include <vtkGenericOpenGLRenderWindow.h>
#include <vtkOpenGLRenderWindow.h>
#include <vtkRenderWindow.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <algorithm>
#include <cstring>

#ifndef GL_FRAMEBUFFER
#define GL_FRAMEBUFFER 0x8D40
#endif

#ifndef GL_COLOR_ATTACHMENT0
#define GL_COLOR_ATTACHMENT0 0x8CE0
#endif

#ifndef GL_FRAMEBUFFER_COMPLETE
#define GL_FRAMEBUFFER_COMPLETE 0x8CD5
#endif

#ifndef WGL_ACCESS_WRITE_DISCARD_NV
#define WGL_ACCESS_WRITE_DISCARD_NV 0x0002
#endif

namespace
{
using glGenFramebuffersProc = void(APIENTRY*)(GLsizei, GLuint*);
using glBindFramebufferProc = void(APIENTRY*)(GLenum, GLuint);
using glFramebufferTexture2DProc = void(APIENTRY*)(GLenum, GLenum, GLenum, GLuint, GLint);
using glCheckFramebufferStatusProc = GLenum(APIENTRY*)(GLenum);
using glDeleteFramebuffersProc = void(APIENTRY*)(GLsizei, const GLuint*);

using wglDXSetResourceShareHandleNVProc = BOOL(WINAPI*)(void*, HANDLE);
using wglDXOpenDeviceNVProc = HANDLE(WINAPI*)(void*);
using wglDXCloseDeviceNVProc = BOOL(WINAPI*)(HANDLE);
using wglDXRegisterObjectNVProc = HANDLE(WINAPI*)(HANDLE, void*, GLuint, GLenum, GLenum);
using wglDXUnregisterObjectNVProc = BOOL(WINAPI*)(HANDLE, HANDLE);
using wglDXLockObjectsNVProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);
using wglDXUnlockObjectsNVProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);

thread_local char LastRenderTargetError[256] = {};

template <typename T>
void ReleaseCom(T*& value)
{
    if (value)
    {
        value->Release();
        value = nullptr;
    }
}

template <typename T>
T LoadWglProc(const char* name)
{
    return reinterpret_cast<T>(::wglGetProcAddress(name));
}

LRESULT CALLBACK RenderTargetWindowProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
{
    return ::DefWindowProc(hwnd, message, wparam, lparam);
}
}

class VtkWpfD3DImageOpenGLRenderTarget::Impl
{
public:
    HWND Window = nullptr;
    HDC DeviceContext = nullptr;
    HGLRC GlContext = nullptr;
    HMODULE OpenGL32Library = nullptr;

    IDirect3D9Ex* Direct3D = nullptr;
    IDirect3DDevice9Ex* D3DDevice = nullptr;
    IDirect3DTexture9* Texture = nullptr;
    IDirect3DSurface9* Surface = nullptr;

    HANDLE DxInteropDevice = nullptr;
    HANDLE DxInteropObject = nullptr;

    GLuint GlTexture = 0;
    GLuint Framebuffer = 0;

    vtkSmartPointer<vtkGenericOpenGLRenderWindow> RenderWindow;
    vtkSmartPointer<vtkRenderer> Renderer;
    vtkSmartPointer<vtkCallbackCommand> MakeCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> IsCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> SupportsOpenGLCallback;
    vtkSmartPointer<vtkCallbackCommand> IsDirectCallback;
    vtkSmartPointer<vtkCallbackCommand> FrameCallback;

    int Width = 1;
    int Height = 1;

    glGenFramebuffersProc glGenFramebuffers = nullptr;
    glBindFramebufferProc glBindFramebuffer = nullptr;
    glFramebufferTexture2DProc glFramebufferTexture2D = nullptr;
    glCheckFramebufferStatusProc glCheckFramebufferStatus = nullptr;
    glDeleteFramebuffersProc glDeleteFramebuffers = nullptr;

    wglDXSetResourceShareHandleNVProc wglDXSetResourceShareHandleNV = nullptr;
    wglDXOpenDeviceNVProc wglDXOpenDeviceNV = nullptr;
    wglDXCloseDeviceNVProc wglDXCloseDeviceNV = nullptr;
    wglDXRegisterObjectNVProc wglDXRegisterObjectNV = nullptr;
    wglDXUnregisterObjectNVProc wglDXUnregisterObjectNV = nullptr;
    wglDXLockObjectsNVProc wglDXLockObjectsNV = nullptr;
    wglDXUnlockObjectsNVProc wglDXUnlockObjectsNV = nullptr;

    bool Initialize()
    {
        return this->CreateHiddenOpenGLContext() &&
            this->LoadOpenGLExtensions() &&
            this->CreateD3DDevice() &&
            this->OpenDxInteropDevice() &&
            this->InitializeVtk() &&
            this->CreateInteropResource(this->Width, this->Height);
    }

    void Release()
    {
        if (this->GlContext)
        {
            ::wglMakeCurrent(this->DeviceContext, this->GlContext);
        }

        this->Renderer = nullptr;
        this->RenderWindow = nullptr;
        this->MakeCurrentCallback = nullptr;
        this->IsCurrentCallback = nullptr;
        this->SupportsOpenGLCallback = nullptr;
        this->IsDirectCallback = nullptr;
        this->FrameCallback = nullptr;

        this->ReleaseInteropResource();

        if (this->DxInteropDevice)
        {
            this->wglDXCloseDeviceNV(this->DxInteropDevice);
            this->DxInteropDevice = nullptr;
        }

        ReleaseCom(this->D3DDevice);
        ReleaseCom(this->Direct3D);

        if (this->GlContext)
        {
            ::wglMakeCurrent(nullptr, nullptr);
            ::wglDeleteContext(this->GlContext);
            this->GlContext = nullptr;
        }

        if (this->OpenGL32Library)
        {
            ::FreeLibrary(this->OpenGL32Library);
            this->OpenGL32Library = nullptr;
        }

        if (this->DeviceContext && this->Window)
        {
            ::ReleaseDC(this->Window, this->DeviceContext);
            this->DeviceContext = nullptr;
        }

        if (this->Window)
        {
            ::DestroyWindow(this->Window);
            this->Window = nullptr;
        }
    }

    void SetSize(int width, int height)
    {
        const int clampedWidth = std::max(1, width);
        const int clampedHeight = std::max(1, height);

        if (this->Surface && this->Width == clampedWidth && this->Height == clampedHeight)
        {
            return;
        }

        this->CreateInteropResource(clampedWidth, clampedHeight);
    }

    void Render()
    {
        if (!this->DxInteropObject) return;

        ::wglMakeCurrent(this->DeviceContext, this->GlContext);

        HANDLE object = this->DxInteropObject;
        if (!this->wglDXLockObjectsNV(this->DxInteropDevice, 1, &object))
        {
            this->SetError("wglDXLockObjectsNV failed.");
            return;
        }

        this->glBindFramebuffer(GL_FRAMEBUFFER, this->Framebuffer);
        this->glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, this->GlTexture, 0);

        if (this->glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE)
        {
            ::glViewport(0, 0, this->Width, this->Height);
            this->RenderWindow->Render();
            ::glFinish();
        }
        else
        {
            this->SetError("Shared OpenGL framebuffer is incomplete.");
        }

        this->glBindFramebuffer(GL_FRAMEBUFFER, 0);
        this->wglDXUnlockObjectsNV(this->DxInteropDevice, 1, &object);
    }

    vtkOpenGLRenderWindow::VTKOpenGLAPIProc LoadOpenGLSymbol(const char* name)
    {
        auto* proc = reinterpret_cast<void*>(::wglGetProcAddress(name));

        if (proc == nullptr || proc == reinterpret_cast<void*>(1) || proc == reinterpret_cast<void*>(2) ||
            proc == reinterpret_cast<void*>(3) || proc == reinterpret_cast<void*>(-1))
        {
            proc = reinterpret_cast<void*>(::GetProcAddress(this->OpenGL32Library, name));
        }

        return reinterpret_cast<vtkOpenGLRenderWindow::VTKOpenGLAPIProc>(proc);
    }

private:
    void SetError(const char* message)
    {
        std::strncpy(LastRenderTargetError, message, sizeof(LastRenderTargetError) - 1);
        LastRenderTargetError[sizeof(LastRenderTargetError) - 1] = '\0';
    }

    bool CheckHr(HRESULT hr, const char* message)
    {
        if (FAILED(hr))
        {
            this->SetError(message);
            return false;
        }

        return true;
    }

    bool CreateHiddenOpenGLContext()
    {
        const wchar_t* className = L"VtkSharpD3DImageOpenGLRenderTargetWindow";

        WNDCLASSW windowClass = {};
        windowClass.lpfnWndProc = RenderTargetWindowProc;
        windowClass.hInstance = ::GetModuleHandleW(nullptr);
        windowClass.lpszClassName = className;
        ::RegisterClassW(&windowClass);

        this->Window = ::CreateWindowExW(0, className, L"VtkSharp D3DImage OpenGL Render Target",
            WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1, 1, nullptr, nullptr,
            ::GetModuleHandleW(nullptr), nullptr);
        if (!this->Window)
        {
            this->SetError("CreateWindowExW failed.");
            return false;
        }

        this->DeviceContext = ::GetDC(this->Window);
        if (!this->DeviceContext)
        {
            this->SetError("GetDC failed.");
            return false;
        }

        PIXELFORMATDESCRIPTOR pixelFormat = {};
        pixelFormat.nSize = sizeof(pixelFormat);
        pixelFormat.nVersion = 1;
        pixelFormat.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
        pixelFormat.iPixelType = PFD_TYPE_RGBA;
        pixelFormat.cColorBits = 32;
        pixelFormat.cDepthBits = 24;
        pixelFormat.iLayerType = PFD_MAIN_PLANE;

        const int format = ::ChoosePixelFormat(this->DeviceContext, &pixelFormat);
        if (format == 0 || !::SetPixelFormat(this->DeviceContext, format, &pixelFormat))
        {
            this->SetError("Failed to set an OpenGL pixel format.");
            return false;
        }

        this->GlContext = ::wglCreateContext(this->DeviceContext);
        if (!this->GlContext || !::wglMakeCurrent(this->DeviceContext, this->GlContext))
        {
            this->SetError("Failed to create or activate an OpenGL context.");
            return false;
        }

        this->OpenGL32Library = ::LoadLibraryW(L"opengl32.dll");
        if (!this->OpenGL32Library)
        {
            this->SetError("LoadLibraryW(opengl32.dll) failed.");
            return false;
        }

        return true;
    }

    bool LoadOpenGLExtensions()
    {
        this->glGenFramebuffers = LoadWglProc<glGenFramebuffersProc>("glGenFramebuffers");
        this->glBindFramebuffer = LoadWglProc<glBindFramebufferProc>("glBindFramebuffer");
        this->glFramebufferTexture2D = LoadWglProc<glFramebufferTexture2DProc>("glFramebufferTexture2D");
        this->glCheckFramebufferStatus = LoadWglProc<glCheckFramebufferStatusProc>("glCheckFramebufferStatus");
        this->glDeleteFramebuffers = LoadWglProc<glDeleteFramebuffersProc>("glDeleteFramebuffers");

        this->wglDXSetResourceShareHandleNV = LoadWglProc<wglDXSetResourceShareHandleNVProc>("wglDXSetResourceShareHandleNV");
        this->wglDXOpenDeviceNV = LoadWglProc<wglDXOpenDeviceNVProc>("wglDXOpenDeviceNV");
        this->wglDXCloseDeviceNV = LoadWglProc<wglDXCloseDeviceNVProc>("wglDXCloseDeviceNV");
        this->wglDXRegisterObjectNV = LoadWglProc<wglDXRegisterObjectNVProc>("wglDXRegisterObjectNV");
        this->wglDXUnregisterObjectNV = LoadWglProc<wglDXUnregisterObjectNVProc>("wglDXUnregisterObjectNV");
        this->wglDXLockObjectsNV = LoadWglProc<wglDXLockObjectsNVProc>("wglDXLockObjectsNV");
        this->wglDXUnlockObjectsNV = LoadWglProc<wglDXUnlockObjectsNVProc>("wglDXUnlockObjectsNV");

        if (!this->glGenFramebuffers || !this->glBindFramebuffer || !this->glFramebufferTexture2D ||
            !this->glCheckFramebufferStatus || !this->glDeleteFramebuffers)
        {
            this->SetError("OpenGL framebuffer functions are not available.");
            return false;
        }

        if (!this->wglDXOpenDeviceNV || !this->wglDXCloseDeviceNV || !this->wglDXRegisterObjectNV ||
            !this->wglDXUnregisterObjectNV || !this->wglDXLockObjectsNV || !this->wglDXUnlockObjectsNV)
        {
            this->SetError("WGL_NV_DX_interop is not available.");
            return false;
        }

        return true;
    }

    bool CreateD3DDevice()
    {
        if (!this->CheckHr(::Direct3DCreate9Ex(D3D_SDK_VERSION, &this->Direct3D), "Direct3DCreate9Ex failed."))
        {
            return false;
        }

        D3DPRESENT_PARAMETERS presentParameters = {};
        presentParameters.Windowed = TRUE;
        presentParameters.SwapEffect = D3DSWAPEFFECT_DISCARD;
        presentParameters.hDeviceWindow = ::GetDesktopWindow();
        presentParameters.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
        presentParameters.BackBufferFormat = D3DFMT_A8R8G8B8;
        presentParameters.BackBufferWidth = 1;
        presentParameters.BackBufferHeight = 1;

        return this->CheckHr(
            this->Direct3D->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, ::GetDesktopWindow(),
                D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
                &presentParameters, nullptr, &this->D3DDevice),
            "IDirect3D9Ex::CreateDeviceEx failed.");
    }

    bool OpenDxInteropDevice()
    {
        this->DxInteropDevice = this->wglDXOpenDeviceNV(this->D3DDevice);
        if (!this->DxInteropDevice)
        {
            this->SetError("wglDXOpenDeviceNV failed.");
            return false;
        }

        return true;
    }

    bool InitializeVtk()
    {
        this->RenderWindow = vtkSmartPointer<vtkGenericOpenGLRenderWindow>::New();
        this->Renderer = vtkSmartPointer<vtkRenderer>::New();

        this->MakeCurrentCallback = this->CreateCallback(&Impl::OnMakeCurrent);
        this->RenderWindow->AddObserver(vtkCommand::WindowMakeCurrentEvent, this->MakeCurrentCallback);

        this->IsCurrentCallback = this->CreateCallback(&Impl::OnIsCurrent);
        this->RenderWindow->AddObserver(vtkCommand::WindowIsCurrentEvent, this->IsCurrentCallback);

        this->SupportsOpenGLCallback = this->CreateCallback(&Impl::OnSupportsOpenGL);
        this->RenderWindow->AddObserver(vtkCommand::WindowSupportsOpenGLEvent, this->SupportsOpenGLCallback);

        this->IsDirectCallback = this->CreateCallback(&Impl::OnIsDirect);
        this->RenderWindow->AddObserver(vtkCommand::WindowIsDirectEvent, this->IsDirectCallback);

        this->FrameCallback = this->CreateCallback(&Impl::OnFrame);
        this->RenderWindow->AddObserver(vtkCommand::WindowFrameEvent, this->FrameCallback);

        this->RenderWindow->SetOpenGLSymbolLoader(
            [](void* userData, const char* name) -> vtkOpenGLRenderWindow::VTKOpenGLAPIProc {
                return static_cast<Impl*>(userData)->LoadOpenGLSymbol(name);
            },
            this);
        this->RenderWindow->AddRenderer(this->Renderer);
        this->RenderWindow->SetFrameBlitModeToBlitToCurrent();
        this->RenderWindow->FramebufferFlipYOff();
        return this->RenderWindow->InitializeFromCurrentContext();
    }

    bool CreateInteropResource(int width, int height)
    {
        this->ReleaseInteropResource();

        this->Width = std::max(1, width);
        this->Height = std::max(1, height);

        HANDLE shareHandle = nullptr;
        if (!this->CheckHr(
                this->D3DDevice->CreateTexture(this->Width, this->Height, 1, D3DUSAGE_RENDERTARGET,
                    D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &this->Texture, &shareHandle),
                "IDirect3DDevice9Ex::CreateTexture failed."))
        {
            return false;
        }

        if (this->wglDXSetResourceShareHandleNV)
        {
            this->wglDXSetResourceShareHandleNV(this->Texture, shareHandle);
        }

        if (!this->CheckHr(this->Texture->GetSurfaceLevel(0, &this->Surface), "IDirect3DTexture9::GetSurfaceLevel failed."))
        {
            return false;
        }

        ::glGenTextures(1, &this->GlTexture);
        ::glBindTexture(GL_TEXTURE_2D, this->GlTexture);
        ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        ::glBindTexture(GL_TEXTURE_2D, 0);

        this->DxInteropObject = this->wglDXRegisterObjectNV(
            this->DxInteropDevice, this->Texture, this->GlTexture, GL_TEXTURE_2D, WGL_ACCESS_WRITE_DISCARD_NV);
        if (!this->DxInteropObject)
        {
            this->SetError("wglDXRegisterObjectNV failed.");
            return false;
        }

        this->glGenFramebuffers(1, &this->Framebuffer);
        this->RenderWindow->SetSize(this->Width, this->Height);
        return true;
    }

    void ReleaseInteropResource()
    {
        if (this->DxInteropObject)
        {
            this->wglDXUnregisterObjectNV(this->DxInteropDevice, this->DxInteropObject);
            this->DxInteropObject = nullptr;
        }

        if (this->Framebuffer)
        {
            this->glDeleteFramebuffers(1, &this->Framebuffer);
            this->Framebuffer = 0;
        }

        if (this->GlTexture)
        {
            ::glDeleteTextures(1, &this->GlTexture);
            this->GlTexture = 0;
        }

        ReleaseCom(this->Surface);
        ReleaseCom(this->Texture);
    }

    using CallbackMethod = void (Impl::*)(void*);

    vtkSmartPointer<vtkCallbackCommand> CreateCallback(CallbackMethod method)
    {
        auto callback = vtkSmartPointer<vtkCallbackCommand>::New();
        callback->SetClientData(new CallbackState{this, method});
        callback->SetCallback(&Impl::InvokeCallback);
        callback->SetClientDataDeleteCallback(&Impl::DeleteCallbackState);
        return callback;
    }

    struct CallbackState
    {
        Impl* Target;
        CallbackMethod Method;
    };

    static void InvokeCallback(vtkObject*, unsigned long, void* clientData, void* callData)
    {
        auto* state = static_cast<CallbackState*>(clientData);
        (state->Target->*state->Method)(callData);
    }

    static void DeleteCallbackState(void* clientData)
    {
        delete static_cast<CallbackState*>(clientData);
    }

    void OnMakeCurrent(void*)
    {
        ::wglMakeCurrent(this->DeviceContext, this->GlContext);
    }

    void OnIsCurrent(void* callData)
    {
        auto* isCurrent = static_cast<bool*>(callData);
        if (isCurrent)
        {
            *isCurrent = ::wglGetCurrentContext() == this->GlContext;
        }
    }

    void OnSupportsOpenGL(void* callData)
    {
        auto* supportsOpenGL = static_cast<int*>(callData);
        if (supportsOpenGL)
        {
            *supportsOpenGL = 1;
        }
    }

    void OnIsDirect(void* callData)
    {
        auto* isDirect = static_cast<int*>(callData);
        if (isDirect)
        {
            *isDirect = 1;
        }
    }

    void OnFrame(void*)
    {
        ::glFlush();
    }
};

VtkWpfD3DImageOpenGLRenderTarget* VtkWpfD3DImageOpenGLRenderTarget::Create()
{
    auto* target = new VtkWpfD3DImageOpenGLRenderTarget();
    if (!target->impl_->Initialize())
    {
        delete target;
        return nullptr;
    }

    return target;
}

const char* VtkWpfD3DImageOpenGLRenderTarget::GetLastError()
{
    return LastRenderTargetError;
}

VtkWpfD3DImageOpenGLRenderTarget::VtkWpfD3DImageOpenGLRenderTarget()
    : impl_(new Impl())
{
}

VtkWpfD3DImageOpenGLRenderTarget::~VtkWpfD3DImageOpenGLRenderTarget()
{
    this->impl_->Release();
    delete this->impl_;
}

vtkRenderWindow* VtkWpfD3DImageOpenGLRenderTarget::GetRenderWindow() const
{
    return this->impl_->RenderWindow.GetPointer();
}

vtkRenderer* VtkWpfD3DImageOpenGLRenderTarget::GetRenderer() const
{
    return this->impl_->Renderer.GetPointer();
}

IDirect3DSurface9* VtkWpfD3DImageOpenGLRenderTarget::GetBackBuffer() const
{
    return this->impl_->Surface;
}

void VtkWpfD3DImageOpenGLRenderTarget::SetSize(int width, int height)
{
    this->impl_->SetSize(width, height);
}

void VtkWpfD3DImageOpenGLRenderTarget::Render()
{
    this->impl_->Render();
}
