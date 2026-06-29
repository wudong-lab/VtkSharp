#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include "WglDxInteropApi.h"

namespace
{
template <typename T>
T LoadWglProc(const char* name)
{
    return reinterpret_cast<T>(::wglGetProcAddress(name));
}
}

bool WglDxInteropApi::Load()
{
    this->m_setResourceShareHandle = LoadWglProc<SetResourceShareHandleProc>("wglDXSetResourceShareHandleNV");
    this->m_openDevice = LoadWglProc<OpenDeviceProc>("wglDXOpenDeviceNV");
    this->m_closeDevice = LoadWglProc<CloseDeviceProc>("wglDXCloseDeviceNV");
    this->m_registerObject = LoadWglProc<RegisterObjectProc>("wglDXRegisterObjectNV");
    this->m_unregisterObject = LoadWglProc<UnregisterObjectProc>("wglDXUnregisterObjectNV");
    this->m_lockObjects = LoadWglProc<LockObjectsProc>("wglDXLockObjectsNV");
    this->m_unlockObjects = LoadWglProc<UnlockObjectsProc>("wglDXUnlockObjectsNV");
    return this->IsAvailable();
}

bool WglDxInteropApi::IsAvailable() const
{
    return this->m_openDevice &&
        this->m_closeDevice &&
        this->m_registerObject &&
        this->m_unregisterObject &&
        this->m_lockObjects &&
        this->m_unlockObjects;
}
