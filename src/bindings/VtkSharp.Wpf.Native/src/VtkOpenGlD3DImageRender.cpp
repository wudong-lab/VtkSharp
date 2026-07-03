#include "VtkOpenGlD3DImageRender.h"

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
    return reinterpret_cast<vtkRenderWindow*>(
        VtkSharpExternalOpenGlRenderContext_GetRenderWindow(this->m_vtkContext));
}

vtkRenderer* VtkOpenGlD3DImageRender::GetRenderer() const
{
    return reinterpret_cast<vtkRenderer*>(
        VtkSharpExternalOpenGlRenderContext_GetRenderer(this->m_vtkContext));
}

IDirect3DSurface9* VtkOpenGlD3DImageRender::GetBackBuffer() const
{
    return this->m_d3DRenderTarget.GetSurface();
}

bool VtkOpenGlD3DImageRender::SetSize(int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (this->m_d3DRenderTarget.GetSurface() && this->m_width == clampedWidth && this->m_height == clampedHeight)
    {
        return true;
    }

    return this->CreateInteropResource(clampedWidth, clampedHeight);
}

bool VtkOpenGlD3DImageRender::Render()
{
    if (!this->m_wglContext.MakeCurrent())
    {
        this->SetError("Failed to make OpenGL context current.");
        return false;
    }

    if (!this->m_wglDxInterop.LockObject())
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        return false;
    }

    bool rendered = true;
    if (!this->m_openGlFramebuffer.RenderToTexture(
        this->m_width,
        this->m_height,
        &VtkOpenGlD3DImageRender::RenderVtkWindowCallback,
        this))
    {
        this->SetError("Shared OpenGL framebuffer is incomplete.");
        rendered = false;
    }

    this->m_wglDxInterop.UnlockObject();
    return rendered;
}

void VtkOpenGlD3DImageRender::RenderVtkWindow()
{
    VtkSharpExternalOpenGlRenderContext_Render(this->m_vtkContext);
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
    const bool isCurrent = this->m_wglContext.MakeCurrent();

    if (this->m_vtkContext)
    {
        VtkSharpExternalOpenGlRenderContext_Delete(this->m_vtkContext);
        this->m_vtkContext = nullptr;
    }

    if (isCurrent)
    {
        this->ReleaseInteropResource();
    }

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
    this->m_vtkContext = VtkSharpExternalOpenGlRenderContext_New(
        this,
        &VtkOpenGlD3DImageRender::LoadOpenGlSymbolCallback,
        &VtkOpenGlD3DImageRender::MakeCurrentCallback,
        &VtkOpenGlD3DImageRender::IsCurrentCallback,
        &VtkOpenGlD3DImageRender::FrameCallback);
    if (!this->m_vtkContext)
    {
        this->SetError("Failed to create VTK external OpenGL render context.");
        return false;
    }

    if (!VtkSharpExternalOpenGlRenderContext_InitializeFromCurrentContext(this->m_vtkContext))
    {
        this->SetError(VtkSharpExternalOpenGlRenderContext_GetLastError(this->m_vtkContext));
        return false;
    }

    return true;
}

bool VtkOpenGlD3DImageRender::CreateInteropResource(int width, int height)
{
    this->ReleaseInteropResource();

    this->m_width = std::max(1, width);
    this->m_height = std::max(1, height);

    if (!this->m_d3DRenderTarget.CreateTexture(this->m_width, this->m_height))
    {
        this->SetError(this->m_d3DRenderTarget.GetLastError());
        this->ReleaseInteropResource();
        return false;
    }

    if (!this->m_wglDxInterop.SetResourceShareHandle(
        this->m_d3DRenderTarget.GetTexture(),
        this->m_d3DRenderTarget.GetShareHandle()))
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        this->ReleaseInteropResource();
        return false;
    }

    if (!this->m_openGlFramebuffer.Create())
    {
        this->SetError("Failed to create shared OpenGL framebuffer.");
        this->ReleaseInteropResource();
        return false;
    }

    if (!this->m_wglDxInterop.RegisterObject(
        this->m_d3DRenderTarget.GetTexture(),
        this->m_openGlFramebuffer.GetTexture()))
    {
        this->SetError(this->m_wglDxInterop.GetLastError());
        this->ReleaseInteropResource();
        return false;
    }

    VtkSharpExternalOpenGlRenderContext_SetSize(this->m_vtkContext, this->m_width, this->m_height);
    return true;
}

void VtkOpenGlD3DImageRender::ReleaseInteropResource()
{
    this->m_wglDxInterop.UnregisterObject();
    this->m_openGlFramebuffer.Release();

    this->m_d3DRenderTarget.ReleaseTexture();
}

void* VtkOpenGlD3DImageRender::LoadOpenGlSymbolCallback(void* userData, const char* name)
{
    return static_cast<VtkOpenGlD3DImageRender*>(userData)->m_wglContext.LoadSymbol(name);
}

int VtkOpenGlD3DImageRender::MakeCurrentCallback(void* userData)
{
    return static_cast<VtkOpenGlD3DImageRender*>(userData)->m_wglContext.MakeCurrent() ? 1 : 0;
}

int VtkOpenGlD3DImageRender::IsCurrentCallback(void* userData)
{
    return static_cast<VtkOpenGlD3DImageRender*>(userData)->m_wglContext.IsCurrent() ? 1 : 0;
}

void VtkOpenGlD3DImageRender::FrameCallback(void*)
{
    ::glFlush();
}
