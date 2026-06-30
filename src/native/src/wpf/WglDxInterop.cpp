#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include "WglDxInterop.h"

#include <cstring>

#ifndef WGL_ACCESS_WRITE_DISCARD_NV
#define WGL_ACCESS_WRITE_DISCARD_NV 0x0002
#endif

namespace
{
    template <typename T>
    T LoadWglProc(const char* name)
    {
        return reinterpret_cast<T>(::wglGetProcAddress(name));
    }
}

bool WglDxInterop::Load()
{
    this->wglDXSetResourceShareHandleNV = LoadWglProc<SetResourceShareHandleProc>("wglDXSetResourceShareHandleNV");
    this->wglDXOpenDeviceNV = LoadWglProc<OpenDeviceProc>("wglDXOpenDeviceNV");
    this->wglDXCloseDeviceNV = LoadWglProc<CloseDeviceProc>("wglDXCloseDeviceNV");
    this->wglDXRegisterObjectNV = LoadWglProc<RegisterObjectProc>("wglDXRegisterObjectNV");
    this->wglDXUnregisterObjectNV = LoadWglProc<UnregisterObjectProc>("wglDXUnregisterObjectNV");
    this->wglDXLockObjectsNV = LoadWglProc<LockObjectsProc>("wglDXLockObjectsNV");
    this->wglDXUnlockObjectsNV = LoadWglProc<UnlockObjectsProc>("wglDXUnlockObjectsNV");
    return this->IsAvailable();
}

bool WglDxInterop::IsAvailable() const
{
    return this->wglDXOpenDeviceNV &&
        this->wglDXCloseDeviceNV &&
        this->wglDXRegisterObjectNV &&
        this->wglDXUnregisterObjectNV &&
        this->wglDXLockObjectsNV &&
        this->wglDXUnlockObjectsNV;
}

bool WglDxInterop::OpenDevice(void* d3DDevice)
{
    this->CloseDevice();

    this->m_device = this->wglDXOpenDeviceNV(d3DDevice);
    if (!this->m_device)
    {
        this->SetError("wglDXOpenDeviceNV failed.");
        return false;
    }

    return true;
}

bool WglDxInterop::RegisterObject(void* d3DTexture, GLuint glTexture)
{
    this->UnregisterObject();

    GLenum glTextureType = GL_TEXTURE_2D;
    GLenum access = WGL_ACCESS_WRITE_DISCARD_NV;

    this->m_object = this->wglDXRegisterObjectNV(this->m_device, d3DTexture, glTexture, glTextureType, access);
    if (!this->m_object)
    {
        this->SetError("wglDXRegisterObjectNV failed.");
        return false;
    }

    return true;
}

bool WglDxInterop::LockObject()
{
    if (!this->m_object)
    {
        this->SetError("No WGL/DX interop object is registered.");
        return false;
    }

    HANDLE object = this->m_object;
    if (!this->wglDXLockObjectsNV(this->m_device, 1, &object))
    {
        this->SetError("wglDXLockObjectsNV failed.");
        return false;
    }

    return true;
}

void WglDxInterop::UnlockObject()
{
    if (!this->m_object)
    {
        return;
    }

    HANDLE object = this->m_object;
    this->wglDXUnlockObjectsNV(this->m_device, 1, &object);
}

void WglDxInterop::UnregisterObject()
{
    if (this->m_object)
    {
        this->wglDXUnregisterObjectNV(this->m_device, this->m_object);
        this->m_object = nullptr;
    }
}

void WglDxInterop::CloseDevice()
{
    this->UnregisterObject();

    if (this->m_device)
    {
        this->wglDXCloseDeviceNV(this->m_device);
        this->m_device = nullptr;
    }
}

const char* WglDxInterop::GetLastError() const
{
    return this->m_lastError;
}

void WglDxInterop::SetError(const char* message)
{
    std::strncpy(this->m_lastError, message, sizeof(this->m_lastError) - 1);
    this->m_lastError[sizeof(this->m_lastError) - 1] = '\0';
}
