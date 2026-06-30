#include "D3DImageRenderTarget.h"

#include <algorithm>
#include <cstring>

namespace
{
template <typename T>
void ReleaseCom(T*& value)
{
    if (value)
    {
        value->Release();
        value = nullptr;
    }
}
}

bool D3DImageRenderTarget::CreateDevice()
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
            &presentParameters, nullptr, &this->m_device),
        "IDirect3D9Ex::CreateDeviceEx failed.");
}

bool D3DImageRenderTarget::CreateTexture(int width, int height)
{
    this->ReleaseTexture();

    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);
    this->m_shareHandle = nullptr;

    if (!this->CheckHr(
            this->m_device->CreateTexture(clampedWidth, clampedHeight, 1, D3DUSAGE_RENDERTARGET,
                D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &this->m_texture, &this->m_shareHandle),
            "IDirect3DDevice9Ex::CreateTexture failed."))
    {
        return false;
    }

    if (!this->CheckHr(this->m_texture->GetSurfaceLevel(0, &this->m_surface), "IDirect3DTexture9::GetSurfaceLevel failed."))
    {
        this->ReleaseTexture();
        return false;
    }

    return true;
}

void D3DImageRenderTarget::ReleaseTexture()
{
    ReleaseCom(this->m_surface);
    ReleaseCom(this->m_texture);
    this->m_shareHandle = nullptr;
}

void D3DImageRenderTarget::Release()
{
    this->ReleaseTexture();
    ReleaseCom(this->m_device);
    ReleaseCom(this->m_direct3D);
}

IDirect3DDevice9Ex* D3DImageRenderTarget::GetDevice() const
{
    return this->m_device;
}

IDirect3DTexture9* D3DImageRenderTarget::GetTexture() const
{
    return this->m_texture;
}

IDirect3DSurface9* D3DImageRenderTarget::GetSurface() const
{
    return this->m_surface;
}

HANDLE D3DImageRenderTarget::GetShareHandle() const
{
    return this->m_shareHandle;
}

const char* D3DImageRenderTarget::GetLastError() const
{
    return this->m_lastError;
}

bool D3DImageRenderTarget::CheckHr(HRESULT hr, const char* message)
{
    if (FAILED(hr))
    {
        this->SetError(message);
        return false;
    }

    return true;
}

void D3DImageRenderTarget::SetError(const char* message)
{
    std::strncpy(this->m_lastError, message, sizeof(this->m_lastError) - 1);
    this->m_lastError[sizeof(this->m_lastError) - 1] = '\0';
}
