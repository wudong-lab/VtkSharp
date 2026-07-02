#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>

#include <d3d9.h>

class D3DImageRenderTarget
{
public:
    bool CreateDevice();
    bool CreateTexture(int width, int height);

    void ReleaseTexture();
    void Release();

    IDirect3DDevice9Ex* GetDevice() const;
    IDirect3DTexture9* GetTexture() const;
    IDirect3DSurface9* GetSurface() const;
    HANDLE GetShareHandle() const;
    const char* GetLastError() const;

private:
    bool CheckHr(HRESULT hr, const char* message);
    void SetError(const char* message);

    IDirect3D9Ex* m_direct3D = nullptr;
    IDirect3DDevice9Ex* m_device = nullptr;
    IDirect3DTexture9* m_texture = nullptr;
    IDirect3DSurface9* m_surface = nullptr;
    HANDLE m_shareHandle = nullptr;
    char m_lastError[256] = {};
};
