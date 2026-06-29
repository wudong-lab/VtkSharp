#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>

#include <gl/GL.h>

class WglDxInteropApi
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

    SetResourceShareHandleProc m_setResourceShareHandle = nullptr;
    OpenDeviceProc m_openDevice = nullptr;
    CloseDeviceProc m_closeDevice = nullptr;
    RegisterObjectProc m_registerObject = nullptr;
    UnregisterObjectProc m_unregisterObject = nullptr;
    LockObjectsProc m_lockObjects = nullptr;
    UnlockObjectsProc m_unlockObjects = nullptr;
};
