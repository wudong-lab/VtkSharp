#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>

#include <gl/GL.h>

class WglDxInterop
{
public:
    using SetResourceShareHandleProc = BOOL(WINAPI*)(void*, HANDLE);
    using OpenDeviceProc = HANDLE(WINAPI*)(void*);
    using CloseDeviceProc = BOOL(WINAPI*)(HANDLE);
    using RegisterObjectProc = HANDLE(WINAPI*)(HANDLE, void*, GLuint, GLenum, GLenum);
    using UnregisterObjectProc = BOOL(WINAPI*)(HANDLE, HANDLE);
    using LockObjectsProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);
    using UnlockObjectsProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);

    bool Load();
    bool IsAvailable() const;

    bool SetResourceShareHandle(void* d3dTexture, HANDLE sharedHandle);

    bool OpenDevice(void* d3DDevice);
    bool RegisterObject(void* d3DTexture, GLuint glTexture);
    bool LockObject();
    void UnlockObject();
    void UnregisterObject();
    void CloseDevice();
    const char* GetLastError() const;

private:
    SetResourceShareHandleProc wglDXSetResourceShareHandleNV = nullptr;
    OpenDeviceProc wglDXOpenDeviceNV = nullptr;
    CloseDeviceProc wglDXCloseDeviceNV = nullptr;
    RegisterObjectProc wglDXRegisterObjectNV = nullptr;
    UnregisterObjectProc wglDXUnregisterObjectNV = nullptr;
    LockObjectsProc wglDXLockObjectsNV = nullptr;
    UnlockObjectsProc wglDXUnlockObjectsNV = nullptr;

    void SetError(const char* message);

    HANDLE m_device = nullptr;
    HANDLE m_object = nullptr;
    char m_lastError[256] = {};
};
