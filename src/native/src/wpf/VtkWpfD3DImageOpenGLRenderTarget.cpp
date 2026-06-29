#include "VtkWpfD3DImageOpenGLRenderTarget.h"

#include <vtkCallbackCommand.h>
#include <vtkCommand.h>
#include <vtkGenericOpenGLRenderWindow.h>
#include <vtkRenderWindow.h>
#include <vtkRenderer.h>

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

bool VtkWpfD3DImageOpenGLRenderTarget::WglDxInteropApi::Load()
{
    this->m_setResourceShareHandle = LoadWglProc<WglDxSetResourceShareHandleNvProc>("wglDXSetResourceShareHandleNV");
    this->m_openDevice = LoadWglProc<WglDxOpenDeviceNvProc>("wglDXOpenDeviceNV");
    this->m_closeDevice = LoadWglProc<WglDxCloseDeviceNvProc>("wglDXCloseDeviceNV");
    this->m_registerObject = LoadWglProc<WglDxRegisterObjectNvProc>("wglDXRegisterObjectNV");
    this->m_unregisterObject = LoadWglProc<WglDxUnregisterObjectNvProc>("wglDXUnregisterObjectNV");
    this->m_lockObjects = LoadWglProc<WglDxLockObjectsNvProc>("wglDXLockObjectsNV");
    this->m_unlockObjects = LoadWglProc<WglDxUnlockObjectsNvProc>("wglDXUnlockObjectsNV");
    return this->IsAvailable();
}

bool VtkWpfD3DImageOpenGLRenderTarget::WglDxInteropApi::IsAvailable() const
{
    return this->m_openDevice &&
        this->m_closeDevice &&
        this->m_registerObject &&
        this->m_unregisterObject &&
        this->m_lockObjects &&
        this->m_unlockObjects;
}

bool VtkWpfD3DImageOpenGLRenderTarget::WglContext::CreateHiddenWindowContext()
{
    const wchar_t* className = L"VtkSharpD3DImageOpenGLRenderTargetWindow";

    WNDCLASSW windowClass = {};
    windowClass.lpfnWndProc = RenderTargetWindowProc;
    windowClass.hInstance = ::GetModuleHandleW(nullptr);
    windowClass.lpszClassName = className;
    ::RegisterClassW(&windowClass);

    this->m_window = ::CreateWindowExW(0, className, L"VtkSharp D3DImage OpenGL Render Target",
        WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1, 1, nullptr, nullptr,
        ::GetModuleHandleW(nullptr), nullptr);
    if (!this->m_window)
    {
        return false;
    }

    this->m_deviceContext = ::GetDC(this->m_window);
    if (!this->m_deviceContext)
    {
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

    const int format = ::ChoosePixelFormat(this->m_deviceContext, &pixelFormat);
    if (format == 0 || !::SetPixelFormat(this->m_deviceContext, format, &pixelFormat))
    {
        return false;
    }

    this->m_glContext = ::wglCreateContext(this->m_deviceContext);
    if (!this->m_glContext || !::wglMakeCurrent(this->m_deviceContext, this->m_glContext))
    {
        return false;
    }

    this->m_openGL32Library = ::LoadLibraryW(L"opengl32.dll");
    return this->m_openGL32Library != nullptr;
}

void VtkWpfD3DImageOpenGLRenderTarget::WglContext::Release()
{
    if (this->m_glContext)
    {
        ::wglMakeCurrent(nullptr, nullptr);
        ::wglDeleteContext(this->m_glContext);
        this->m_glContext = nullptr;
    }

    if (this->m_openGL32Library)
    {
        ::FreeLibrary(this->m_openGL32Library);
        this->m_openGL32Library = nullptr;
    }

    if (this->m_deviceContext && this->m_window)
    {
        ::ReleaseDC(this->m_window, this->m_deviceContext);
        this->m_deviceContext = nullptr;
    }

    if (this->m_window)
    {
        ::DestroyWindow(this->m_window);
        this->m_window = nullptr;
    }
}

void VtkWpfD3DImageOpenGLRenderTarget::WglContext::MakeCurrent() const
{
    ::wglMakeCurrent(this->m_deviceContext, this->m_glContext);
}

bool VtkWpfD3DImageOpenGLRenderTarget::WglContext::IsCurrent() const
{
    return ::wglGetCurrentContext() == this->m_glContext;
}

vtkOpenGLRenderWindow::VTKOpenGLAPIProc VtkWpfD3DImageOpenGLRenderTarget::WglContext::LoadSymbol(
    const char* name) const
{
    auto* proc = reinterpret_cast<void*>(::wglGetProcAddress(name));

    if (proc == nullptr || proc == reinterpret_cast<void*>(1) || proc == reinterpret_cast<void*>(2) ||
        proc == reinterpret_cast<void*>(3) || proc == reinterpret_cast<void*>(-1))
    {
        proc = reinterpret_cast<void*>(::GetProcAddress(this->m_openGL32Library, name));
    }

    return reinterpret_cast<vtkOpenGLRenderWindow::VTKOpenGLAPIProc>(proc);
}

VtkWpfD3DImageOpenGLRenderTarget* VtkWpfD3DImageOpenGLRenderTarget::Create()
{
    auto* target = new VtkWpfD3DImageOpenGLRenderTarget();
    if (!target->Initialize())
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

VtkWpfD3DImageOpenGLRenderTarget::~VtkWpfD3DImageOpenGLRenderTarget()
{
    this->Release();
}

vtkRenderWindow* VtkWpfD3DImageOpenGLRenderTarget::GetRenderWindow() const
{
    return this->m_renderWindow.GetPointer();
}

vtkRenderer* VtkWpfD3DImageOpenGLRenderTarget::GetRenderer() const
{
    return this->m_renderer.GetPointer();
}

IDirect3DSurface9* VtkWpfD3DImageOpenGLRenderTarget::GetBackBuffer() const
{
    return this->m_surface;
}

void VtkWpfD3DImageOpenGLRenderTarget::SetSize(int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (this->m_surface && this->m_width == clampedWidth && this->m_height == clampedHeight)
    {
        return;
    }

    this->CreateInteropResource(clampedWidth, clampedHeight);
}

void VtkWpfD3DImageOpenGLRenderTarget::Render()
{
    if (!this->m_dxInteropObject) return;

    this->m_wglContext.MakeCurrent();

    HANDLE object = this->m_dxInteropObject;
    if (!this->m_wglDxInteropApi.m_lockObjects(this->m_dxInteropDevice, 1, &object))
    {
        this->SetError("wglDXLockObjectsNV failed.");
        return;
    }

    this->m_glBindFramebuffer(GL_FRAMEBUFFER, this->m_framebuffer);
    this->m_glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, this->m_glTexture, 0);

    if (this->m_glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE)
    {
        ::glViewport(0, 0, this->m_width, this->m_height);
        this->m_renderWindow->Render();
        ::glFinish();
    }
    else
    {
        this->SetError("Shared OpenGL framebuffer is incomplete.");
    }

    this->m_glBindFramebuffer(GL_FRAMEBUFFER, 0);
    this->m_wglDxInteropApi.m_unlockObjects(this->m_dxInteropDevice, 1, &object);
}

bool VtkWpfD3DImageOpenGLRenderTarget::Initialize()
{
    if (!this->m_wglContext.CreateHiddenWindowContext())
    {
        this->SetError("Failed to create hidden OpenGL context.");
        return false;
    }

    this->m_wglContext.MakeCurrent();

    return this->LoadOpenGLExtensions() &&
        this->CreateD3DDevice() &&
        this->OpenDxInteropDevice() &&
        this->InitializeVtk() &&
        this->CreateInteropResource(this->m_width, this->m_height);
}

void VtkWpfD3DImageOpenGLRenderTarget::Release()
{
    this->m_wglContext.MakeCurrent();

    this->m_renderer = nullptr;
    this->m_renderWindow = nullptr;
    this->m_makeCurrentCallback = nullptr;
    this->m_isCurrentCallback = nullptr;
    this->m_supportsOpenGLCallback = nullptr;
    this->m_isDirectCallback = nullptr;
    this->m_frameCallback = nullptr;

    this->ReleaseInteropResource();

    if (this->m_dxInteropDevice)
    {
        this->m_wglDxInteropApi.m_closeDevice(this->m_dxInteropDevice);
        this->m_dxInteropDevice = nullptr;
    }

    ReleaseCom(this->m_d3DDevice);
    ReleaseCom(this->m_direct3D);
    this->m_wglContext.Release();
}

void VtkWpfD3DImageOpenGLRenderTarget::SetError(const char* message)
{
    std::strncpy(LastRenderTargetError, message, sizeof(LastRenderTargetError) - 1);
    LastRenderTargetError[sizeof(LastRenderTargetError) - 1] = '\0';
}

bool VtkWpfD3DImageOpenGLRenderTarget::CheckHr(HRESULT hr, const char* message)
{
    if (FAILED(hr))
    {
        this->SetError(message);
        return false;
    }

    return true;
}

bool VtkWpfD3DImageOpenGLRenderTarget::LoadOpenGLExtensions()
{
    this->m_glGenFramebuffers = LoadWglProc<GlGenFramebuffersProc>("glGenFramebuffers");
    this->m_glBindFramebuffer = LoadWglProc<GlBindFramebufferProc>("glBindFramebuffer");
    this->m_glFramebufferTexture2D = LoadWglProc<GlFramebufferTexture2DProc>("glFramebufferTexture2D");
    this->m_glCheckFramebufferStatus = LoadWglProc<GlCheckFramebufferStatusProc>("glCheckFramebufferStatus");
    this->m_glDeleteFramebuffers = LoadWglProc<GlDeleteFramebuffersProc>("glDeleteFramebuffers");

    if (!this->m_glGenFramebuffers || !this->m_glBindFramebuffer || !this->m_glFramebufferTexture2D ||
        !this->m_glCheckFramebufferStatus || !this->m_glDeleteFramebuffers)
    {
        this->SetError("OpenGL framebuffer functions are not available.");
        return false;
    }

    if (!this->m_wglDxInteropApi.Load())
    {
        this->SetError("WGL_NV_DX_interop is not available.");
        return false;
    }

    return true;
}

bool VtkWpfD3DImageOpenGLRenderTarget::CreateD3DDevice()
{
    if (!this->CheckHr(::Direct3DCreate9Ex(D3D_SDK_VERSION, &this->m_direct3D), "Direct3DCreate9Ex failed."))
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
        this->m_direct3D->CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, ::GetDesktopWindow(),
            D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
            &presentParameters, nullptr, &this->m_d3DDevice),
        "IDirect3D9Ex::CreateDeviceEx failed.");
}

bool VtkWpfD3DImageOpenGLRenderTarget::OpenDxInteropDevice()
{
    this->m_dxInteropDevice = this->m_wglDxInteropApi.m_openDevice(this->m_d3DDevice);
    if (!this->m_dxInteropDevice)
    {
        this->SetError("wglDXOpenDeviceNV failed.");
        return false;
    }

    return true;
}

bool VtkWpfD3DImageOpenGLRenderTarget::InitializeVtk()
{
    this->m_renderWindow = vtkSmartPointer<vtkGenericOpenGLRenderWindow>::New();
    this->m_renderer = vtkSmartPointer<vtkRenderer>::New();

    this->m_makeCurrentCallback = this->CreateCallback(&VtkWpfD3DImageOpenGLRenderTarget::OnMakeCurrent);
    this->m_renderWindow->AddObserver(vtkCommand::WindowMakeCurrentEvent, this->m_makeCurrentCallback);

    this->m_isCurrentCallback = this->CreateCallback(&VtkWpfD3DImageOpenGLRenderTarget::OnIsCurrent);
    this->m_renderWindow->AddObserver(vtkCommand::WindowIsCurrentEvent, this->m_isCurrentCallback);

    this->m_supportsOpenGLCallback = this->CreateCallback(&VtkWpfD3DImageOpenGLRenderTarget::OnSupportsOpenGL);
    this->m_renderWindow->AddObserver(vtkCommand::WindowSupportsOpenGLEvent, this->m_supportsOpenGLCallback);

    this->m_isDirectCallback = this->CreateCallback(&VtkWpfD3DImageOpenGLRenderTarget::OnIsDirect);
    this->m_renderWindow->AddObserver(vtkCommand::WindowIsDirectEvent, this->m_isDirectCallback);

    this->m_frameCallback = this->CreateCallback(&VtkWpfD3DImageOpenGLRenderTarget::OnFrame);
    this->m_renderWindow->AddObserver(vtkCommand::WindowFrameEvent, this->m_frameCallback);

    this->m_renderWindow->SetOpenGLSymbolLoader(
        [](void* userData, const char* name) -> vtkOpenGLRenderWindow::VTKOpenGLAPIProc {
            return static_cast<VtkWpfD3DImageOpenGLRenderTarget*>(userData)->m_wglContext.LoadSymbol(name);
        },
        this);
    this->m_renderWindow->AddRenderer(this->m_renderer);
    this->m_renderWindow->SetFrameBlitModeToBlitToCurrent();
    this->m_renderWindow->FramebufferFlipYOff();
    return this->m_renderWindow->InitializeFromCurrentContext();
}

bool VtkWpfD3DImageOpenGLRenderTarget::CreateInteropResource(int width, int height)
{
    this->ReleaseInteropResource();

    this->m_width = std::max(1, width);
    this->m_height = std::max(1, height);

    HANDLE shareHandle = nullptr;
    if (!this->CheckHr(
            this->m_d3DDevice->CreateTexture(this->m_width, this->m_height, 1, D3DUSAGE_RENDERTARGET,
                D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &this->m_texture, &shareHandle),
            "IDirect3DDevice9Ex::CreateTexture failed."))
    {
        return false;
    }

    if (this->m_wglDxInteropApi.m_setResourceShareHandle)
    {
        this->m_wglDxInteropApi.m_setResourceShareHandle(this->m_texture, shareHandle);
    }

    if (!this->CheckHr(this->m_texture->GetSurfaceLevel(0, &this->m_surface), "IDirect3DTexture9::GetSurfaceLevel failed."))
    {
        return false;
    }

    ::glGenTextures(1, &this->m_glTexture);
    ::glBindTexture(GL_TEXTURE_2D, this->m_glTexture);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    ::glBindTexture(GL_TEXTURE_2D, 0);

    this->m_dxInteropObject = this->m_wglDxInteropApi.m_registerObject(
        this->m_dxInteropDevice, this->m_texture, this->m_glTexture, GL_TEXTURE_2D, WGL_ACCESS_WRITE_DISCARD_NV);
    if (!this->m_dxInteropObject)
    {
        this->SetError("wglDXRegisterObjectNV failed.");
        return false;
    }

    this->m_glGenFramebuffers(1, &this->m_framebuffer);
    this->m_renderWindow->SetSize(this->m_width, this->m_height);
    return true;
}

void VtkWpfD3DImageOpenGLRenderTarget::ReleaseInteropResource()
{
    if (this->m_dxInteropObject)
    {
        this->m_wglDxInteropApi.m_unregisterObject(this->m_dxInteropDevice, this->m_dxInteropObject);
        this->m_dxInteropObject = nullptr;
    }

    if (this->m_framebuffer)
    {
        this->m_glDeleteFramebuffers(1, &this->m_framebuffer);
        this->m_framebuffer = 0;
    }

    if (this->m_glTexture)
    {
        ::glDeleteTextures(1, &this->m_glTexture);
        this->m_glTexture = 0;
    }

    ReleaseCom(this->m_surface);
    ReleaseCom(this->m_texture);
}

vtkSmartPointer<vtkCallbackCommand> VtkWpfD3DImageOpenGLRenderTarget::CreateCallback(CallbackMethod method)
{
    auto callback = vtkSmartPointer<vtkCallbackCommand>::New();
    callback->SetClientData(new CallbackState{this, method});
    callback->SetCallback(&VtkWpfD3DImageOpenGLRenderTarget::InvokeCallback);
    callback->SetClientDataDeleteCallback(&VtkWpfD3DImageOpenGLRenderTarget::DeleteCallbackState);
    return callback;
}

void VtkWpfD3DImageOpenGLRenderTarget::InvokeCallback(
    vtkObject*,
    unsigned long,
    void* clientData,
    void* callData)
{
    auto* state = static_cast<CallbackState*>(clientData);
    (state->Target->*state->Method)(callData);
}

void VtkWpfD3DImageOpenGLRenderTarget::DeleteCallbackState(void* clientData)
{
    delete static_cast<CallbackState*>(clientData);
}

void VtkWpfD3DImageOpenGLRenderTarget::OnMakeCurrent(void*)
{
    this->m_wglContext.MakeCurrent();
}

void VtkWpfD3DImageOpenGLRenderTarget::OnIsCurrent(void* callData)
{
    auto* isCurrent = static_cast<bool*>(callData);
    if (isCurrent)
    {
        *isCurrent = this->m_wglContext.IsCurrent();
    }
}

void VtkWpfD3DImageOpenGLRenderTarget::OnSupportsOpenGL(void* callData)
{
    auto* supportsOpenGL = static_cast<int*>(callData);
    if (supportsOpenGL)
    {
        *supportsOpenGL = 1;
    }
}

void VtkWpfD3DImageOpenGLRenderTarget::OnIsDirect(void* callData)
{
    auto* isDirect = static_cast<int*>(callData);
    if (isDirect)
    {
        *isDirect = 1;
    }
}

void VtkWpfD3DImageOpenGLRenderTarget::OnFrame(void*)
{
    ::glFlush();
}
