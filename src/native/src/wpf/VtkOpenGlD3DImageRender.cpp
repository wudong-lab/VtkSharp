#include "VtkOpenGlD3DImageRender.h"

#include <vtkCallbackCommand.h>
#include <vtkCommand.h>
#include <vtkGenericOpenGLRenderWindow.h>
#include <vtkRenderWindow.h>
#include <vtkRenderer.h>

#include <algorithm>
#include <cstring>

namespace
{
    thread_local char LastRenderTargetError[256] = {};
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
    return this->m_d3DRenderTarget.GetSurface();
}

void VtkOpenGlD3DImageRender::SetSize(int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (this->m_d3DRenderTarget.GetSurface() && this->m_width == clampedWidth && this->m_height == clampedHeight)
    {
        return;
    }

    this->CreateInteropResource(clampedWidth, clampedHeight);
}

void VtkOpenGlD3DImageRender::Render()
{
    this->m_wglContext.MakeCurrent();

    if (!this->m_wglDxInterop.LockObject())
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        return;
    }

    if (!this->m_openGlFramebuffer.RenderToTexture(
        this->m_width,
        this->m_height,
        &VtkOpenGlD3DImageRender::RenderVtkWindowCallback,
        this))
    {
        this->SetError("Shared OpenGL framebuffer is incomplete.");
    }

    this->m_wglDxInterop.UnlockObject();
}

void VtkOpenGlD3DImageRender::RenderVtkWindow()
{
    this->m_renderWindow->Render();
}

void VtkOpenGlD3DImageRender::RenderVtkWindowCallback(void* userData)
{
    static_cast<VtkOpenGlD3DImageRender*>(userData)->RenderVtkWindow();
}

bool VtkOpenGlD3DImageRender::Initialize()
{
    if (!this->m_wglContext.CreateHiddenWindowContext())
    {
        this->SetError("Failed to create hidden OpenGL context.");
        return false;
    }

    this->m_wglContext.MakeCurrent();

    if (!this->LoadOpenGLExtensions())
    {
        return false;
    }

    if (!this->m_d3DRenderTarget.CreateDevice())
    {
        this->SetError(this->m_d3DRenderTarget.GetLastError());
        return false;
    }

    return this->OpenDxInteropDevice() &&
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

    this->m_wglDxInterop.CloseDevice();
    this->m_d3DRenderTarget.Release();
    this->m_wglContext.Release();
}

void VtkOpenGlD3DImageRender::SetError(const char* message)
{
    std::strncpy(LastRenderTargetError, message, sizeof(LastRenderTargetError) - 1);
    LastRenderTargetError[sizeof(LastRenderTargetError) - 1] = '\0';
}

bool VtkOpenGlD3DImageRender::LoadOpenGLExtensions()
{
    if (!this->m_openGlFramebuffer.Load())
    {
        this->SetError("OpenGL framebuffer functions are not available.");
        return false;
    }

    if (!this->m_wglDxInterop.Load())
    {
        this->SetError("WGL_NV_DX_interop is not available.");
        return false;
    }

    return true;
}

bool VtkOpenGlD3DImageRender::OpenDxInteropDevice()
{
    if (!this->m_wglDxInterop.OpenDevice(this->m_d3DRenderTarget.GetDevice()))
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
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
        [](void* userData, const char* name) -> vtkOpenGLRenderWindow::VTKOpenGLAPIProc
        {
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

    if (!this->m_d3DRenderTarget.CreateTexture(this->m_width, this->m_height))
    {
        this->SetError(this->m_d3DRenderTarget.GetLastError());
        return false;
    }

    if (!this->m_wglDxInterop.SetResourceShareHandle(
        this->m_d3DRenderTarget.GetTexture(),
        this->m_d3DRenderTarget.GetShareHandle()))
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        return false;
    }

    this->m_openGlFramebuffer.Create();

    if (!this->m_wglDxInterop.RegisterObject(
        this->m_d3DRenderTarget.GetTexture(),
        this->m_openGlFramebuffer.GetTexture()))
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        return false;
    }

    this->m_renderWindow->SetSize(this->m_width, this->m_height);
    return true;
}

void VtkOpenGlD3DImageRender::ReleaseInteropResource()
{
    this->m_wglDxInterop.UnregisterObject();
    this->m_openGlFramebuffer.Release();

    this->m_d3DRenderTarget.ReleaseTexture();
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
