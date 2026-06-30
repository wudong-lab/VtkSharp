#include "VtkOpenGlD3DImageRender.h"

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
T LoadOpenGLProc(const char* name)
{
    return reinterpret_cast<T>(::wglGetProcAddress(name));
}
}

VtkOpenGlD3DImageRender* VtkOpenGlD3DImageRender::Create()
{
    auto* target = new VtkOpenGlD3DImageRender();
    if (!target->Initialize())
    {
        delete target;
        return nullptr;
    }

    return target;
}

const char* VtkOpenGlD3DImageRender::GetLastError()
{
    return LastRenderTargetError;
}

VtkOpenGlD3DImageRender::~VtkOpenGlD3DImageRender()
{
    this->Release();
}

vtkRenderWindow* VtkOpenGlD3DImageRender::GetRenderWindow() const
{
    return this->m_renderWindow.GetPointer();
}

vtkRenderer* VtkOpenGlD3DImageRender::GetRenderer() const
{
    return this->m_renderer.GetPointer();
}

IDirect3DSurface9* VtkOpenGlD3DImageRender::GetBackBuffer() const
{
    return this->m_surface;
}

void VtkOpenGlD3DImageRender::SetSize(int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (this->m_surface && this->m_width == clampedWidth && this->m_height == clampedHeight)
    {
        return;
    }

    this->CreateInteropResource(clampedWidth, clampedHeight);
}

void VtkOpenGlD3DImageRender::Render()
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

bool VtkOpenGlD3DImageRender::Initialize()
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

void VtkOpenGlD3DImageRender::Release()
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

void VtkOpenGlD3DImageRender::SetError(const char* message)
{
    std::strncpy(LastRenderTargetError, message, sizeof(LastRenderTargetError) - 1);
    LastRenderTargetError[sizeof(LastRenderTargetError) - 1] = '\0';
}

bool VtkOpenGlD3DImageRender::CheckHr(HRESULT hr, const char* message)
{
    if (FAILED(hr))
    {
        this->SetError(message);
        return false;
    }

    return true;
}

bool VtkOpenGlD3DImageRender::LoadOpenGLExtensions()
{
    this->m_glGenFramebuffers = LoadOpenGLProc<GlGenFramebuffersProc>("glGenFramebuffers");
    this->m_glBindFramebuffer = LoadOpenGLProc<GlBindFramebufferProc>("glBindFramebuffer");
    this->m_glFramebufferTexture2D = LoadOpenGLProc<GlFramebufferTexture2DProc>("glFramebufferTexture2D");
    this->m_glCheckFramebufferStatus = LoadOpenGLProc<GlCheckFramebufferStatusProc>("glCheckFramebufferStatus");
    this->m_glDeleteFramebuffers = LoadOpenGLProc<GlDeleteFramebuffersProc>("glDeleteFramebuffers");

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

bool VtkOpenGlD3DImageRender::CreateD3DDevice()
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

bool VtkOpenGlD3DImageRender::OpenDxInteropDevice()
{
    this->m_dxInteropDevice = this->m_wglDxInteropApi.m_openDevice(this->m_d3DDevice);
    if (!this->m_dxInteropDevice)
    {
        this->SetError("wglDXOpenDeviceNV failed.");
        return false;
    }

    return true;
}

bool VtkOpenGlD3DImageRender::InitializeVtk()
{
    this->m_renderWindow = vtkSmartPointer<vtkGenericOpenGLRenderWindow>::New();
    this->m_renderer = vtkSmartPointer<vtkRenderer>::New();

    this->m_makeCurrentCallback = this->CreateCallback(&VtkOpenGlD3DImageRender::OnMakeCurrent);
    this->m_renderWindow->AddObserver(vtkCommand::WindowMakeCurrentEvent, this->m_makeCurrentCallback);

    this->m_isCurrentCallback = this->CreateCallback(&VtkOpenGlD3DImageRender::OnIsCurrent);
    this->m_renderWindow->AddObserver(vtkCommand::WindowIsCurrentEvent, this->m_isCurrentCallback);

    this->m_supportsOpenGLCallback = this->CreateCallback(&VtkOpenGlD3DImageRender::OnSupportsOpenGL);
    this->m_renderWindow->AddObserver(vtkCommand::WindowSupportsOpenGLEvent, this->m_supportsOpenGLCallback);

    this->m_isDirectCallback = this->CreateCallback(&VtkOpenGlD3DImageRender::OnIsDirect);
    this->m_renderWindow->AddObserver(vtkCommand::WindowIsDirectEvent, this->m_isDirectCallback);

    this->m_frameCallback = this->CreateCallback(&VtkOpenGlD3DImageRender::OnFrame);
    this->m_renderWindow->AddObserver(vtkCommand::WindowFrameEvent, this->m_frameCallback);

    this->m_renderWindow->SetOpenGLSymbolLoader(
        [](void* userData, const char* name) -> vtkOpenGLRenderWindow::VTKOpenGLAPIProc {
            return static_cast<VtkOpenGlD3DImageRender*>(userData)->m_wglContext.LoadSymbol(name);
        },
        this);
    this->m_renderWindow->AddRenderer(this->m_renderer);
    this->m_renderWindow->SetFrameBlitModeToBlitToCurrent();
    this->m_renderWindow->FramebufferFlipYOff();
    return this->m_renderWindow->InitializeFromCurrentContext();
}

bool VtkOpenGlD3DImageRender::CreateInteropResource(int width, int height)
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

void VtkOpenGlD3DImageRender::ReleaseInteropResource()
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

vtkSmartPointer<vtkCallbackCommand> VtkOpenGlD3DImageRender::CreateCallback(CallbackMethod method)
{
    auto callback = vtkSmartPointer<vtkCallbackCommand>::New();
    callback->SetClientData(new CallbackState{this, method});
    callback->SetCallback(&VtkOpenGlD3DImageRender::InvokeCallback);
    callback->SetClientDataDeleteCallback(&VtkOpenGlD3DImageRender::DeleteCallbackState);
    return callback;
}

void VtkOpenGlD3DImageRender::InvokeCallback(
    vtkObject*,
    unsigned long,
    void* clientData,
    void* callData)
{
    auto* state = static_cast<CallbackState*>(clientData);
    (state->Target->*state->Method)(callData);
}

void VtkOpenGlD3DImageRender::DeleteCallbackState(void* clientData)
{
    delete static_cast<CallbackState*>(clientData);
}

void VtkOpenGlD3DImageRender::OnMakeCurrent(void*)
{
    this->m_wglContext.MakeCurrent();
}

void VtkOpenGlD3DImageRender::OnIsCurrent(void* callData)
{
    auto* isCurrent = static_cast<bool*>(callData);
    if (isCurrent)
    {
        *isCurrent = this->m_wglContext.IsCurrent();
    }
}

void VtkOpenGlD3DImageRender::OnSupportsOpenGL(void* callData)
{
    auto* supportsOpenGL = static_cast<int*>(callData);
    if (supportsOpenGL)
    {
        *supportsOpenGL = 1;
    }
}

void VtkOpenGlD3DImageRender::OnIsDirect(void* callData)
{
    auto* isDirect = static_cast<int*>(callData);
    if (isDirect)
    {
        *isDirect = 1;
    }
}

void VtkOpenGlD3DImageRender::OnFrame(void*)
{
    ::glFlush();
}
