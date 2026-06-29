#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include "vtksharp_api.h"

#include <d3d9.h>
#include <gl/GL.h>

#include <algorithm>
#include <cmath>
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
using glGenFramebuffersProc = void(APIENTRY*)(GLsizei, GLuint*);
using glBindFramebufferProc = void(APIENTRY*)(GLenum, GLuint);
using glFramebufferTexture2DProc = void(APIENTRY*)(GLenum, GLenum, GLenum, GLuint, GLint);
using glCheckFramebufferStatusProc = GLenum(APIENTRY*)(GLenum);
using glDeleteFramebuffersProc = void(APIENTRY*)(GLsizei, const GLuint*);

using wglDXSetResourceShareHandleNVProc = BOOL(WINAPI*)(void*, HANDLE);
using wglDXOpenDeviceNVProc = HANDLE(WINAPI*)(void*);
using wglDXCloseDeviceNVProc = BOOL(WINAPI*)(HANDLE);
using wglDXRegisterObjectNVProc = HANDLE(WINAPI*)(HANDLE, void*, GLuint, GLenum, GLenum);
using wglDXUnregisterObjectNVProc = BOOL(WINAPI*)(HANDLE, HANDLE);
using wglDXLockObjectsNVProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);
using wglDXUnlockObjectsNVProc = BOOL(WINAPI*)(HANDLE, GLint, HANDLE*);

thread_local char LastProbeError[256] = {};

struct VtkWpfOpenGLD3DImageProbeBridge
{
    HWND Window = nullptr;
    HDC DeviceContext = nullptr;
    HGLRC GlContext = nullptr;

    IDirect3D9Ex* Direct3D = nullptr;
    IDirect3DDevice9Ex* D3DDevice = nullptr;
    IDirect3DTexture9* Texture = nullptr;
    IDirect3DSurface9* Surface = nullptr;

    HANDLE DxInteropDevice = nullptr;
    HANDLE DxInteropObject = nullptr;

    GLuint GlTexture = 0;
    GLuint Framebuffer = 0;

    int Width = 1;
    int Height = 1;
    int FrameIndex = 0;

    char LastError[256] = {};

    glGenFramebuffersProc glGenFramebuffers = nullptr;
    glBindFramebufferProc glBindFramebuffer = nullptr;
    glFramebufferTexture2DProc glFramebufferTexture2D = nullptr;
    glCheckFramebufferStatusProc glCheckFramebufferStatus = nullptr;
    glDeleteFramebuffersProc glDeleteFramebuffers = nullptr;

    wglDXSetResourceShareHandleNVProc wglDXSetResourceShareHandleNV = nullptr;
    wglDXOpenDeviceNVProc wglDXOpenDeviceNV = nullptr;
    wglDXCloseDeviceNVProc wglDXCloseDeviceNV = nullptr;
    wglDXRegisterObjectNVProc wglDXRegisterObjectNV = nullptr;
    wglDXUnregisterObjectNVProc wglDXUnregisterObjectNV = nullptr;
    wglDXLockObjectsNVProc wglDXLockObjectsNV = nullptr;
    wglDXUnlockObjectsNVProc wglDXUnlockObjectsNV = nullptr;
};

template <typename T>
void ReleaseCom(T*& value)
{
    if (value)
    {
        value->Release();
        value = nullptr;
    }
}

void SetError(VtkWpfOpenGLD3DImageProbeBridge* bridge, const char* message)
{
    std::strncpy(LastProbeError, message, sizeof(LastProbeError) - 1);
    LastProbeError[sizeof(LastProbeError) - 1] = '\0';

    if (!bridge)
    {
        return;
    }

    std::strncpy(bridge->LastError, message, sizeof(bridge->LastError) - 1);
    bridge->LastError[sizeof(bridge->LastError) - 1] = '\0';
}

bool CheckHr(VtkWpfOpenGLD3DImageProbeBridge* bridge, HRESULT hr, const char* message)
{
    if (FAILED(hr))
    {
        SetError(bridge, message);
        return false;
    }

    return true;
}

template <typename T>
T LoadWglProc(const char* name)
{
    return reinterpret_cast<T>(wglGetProcAddress(name));
}

LRESULT CALLBACK ProbeWindowProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
{
    return DefWindowProc(hwnd, message, wparam, lparam);
}

bool CreateHiddenOpenGLContext(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    const wchar_t* className = L"VtkSharpOpenGLD3DImageProbeWindow";

    WNDCLASSW windowClass = {};
    windowClass.lpfnWndProc = ProbeWindowProc;
    windowClass.hInstance = GetModuleHandleW(nullptr);
    windowClass.lpszClassName = className;
    RegisterClassW(&windowClass);

    bridge->Window = CreateWindowExW(
        0,
        className,
        L"VtkSharp OpenGL D3DImage Probe",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        1,
        1,
        nullptr,
        nullptr,
        GetModuleHandleW(nullptr),
        nullptr);
    if (!bridge->Window)
    {
        SetError(bridge, "CreateWindowExW failed.");
        return false;
    }

    bridge->DeviceContext = GetDC(bridge->Window);
    if (!bridge->DeviceContext)
    {
        SetError(bridge, "GetDC failed.");
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

    const int format = ChoosePixelFormat(bridge->DeviceContext, &pixelFormat);
    if (format == 0 || !SetPixelFormat(bridge->DeviceContext, format, &pixelFormat))
    {
        SetError(bridge, "Failed to set an OpenGL pixel format.");
        return false;
    }

    bridge->GlContext = wglCreateContext(bridge->DeviceContext);
    if (!bridge->GlContext || !wglMakeCurrent(bridge->DeviceContext, bridge->GlContext))
    {
        SetError(bridge, "Failed to create or activate an OpenGL context.");
        return false;
    }

    return true;
}

bool LoadOpenGLExtensions(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    bridge->glGenFramebuffers = LoadWglProc<glGenFramebuffersProc>("glGenFramebuffers");
    bridge->glBindFramebuffer = LoadWglProc<glBindFramebufferProc>("glBindFramebuffer");
    bridge->glFramebufferTexture2D = LoadWglProc<glFramebufferTexture2DProc>("glFramebufferTexture2D");
    bridge->glCheckFramebufferStatus = LoadWglProc<glCheckFramebufferStatusProc>("glCheckFramebufferStatus");
    bridge->glDeleteFramebuffers = LoadWglProc<glDeleteFramebuffersProc>("glDeleteFramebuffers");

    bridge->wglDXSetResourceShareHandleNV = LoadWglProc<wglDXSetResourceShareHandleNVProc>("wglDXSetResourceShareHandleNV");
    bridge->wglDXOpenDeviceNV = LoadWglProc<wglDXOpenDeviceNVProc>("wglDXOpenDeviceNV");
    bridge->wglDXCloseDeviceNV = LoadWglProc<wglDXCloseDeviceNVProc>("wglDXCloseDeviceNV");
    bridge->wglDXRegisterObjectNV = LoadWglProc<wglDXRegisterObjectNVProc>("wglDXRegisterObjectNV");
    bridge->wglDXUnregisterObjectNV = LoadWglProc<wglDXUnregisterObjectNVProc>("wglDXUnregisterObjectNV");
    bridge->wglDXLockObjectsNV = LoadWglProc<wglDXLockObjectsNVProc>("wglDXLockObjectsNV");
    bridge->wglDXUnlockObjectsNV = LoadWglProc<wglDXUnlockObjectsNVProc>("wglDXUnlockObjectsNV");

    if (!bridge->glGenFramebuffers || !bridge->glBindFramebuffer || !bridge->glFramebufferTexture2D ||
        !bridge->glCheckFramebufferStatus || !bridge->glDeleteFramebuffers)
    {
        SetError(bridge, "OpenGL framebuffer functions are not available.");
        return false;
    }

    if (!bridge->wglDXOpenDeviceNV || !bridge->wglDXCloseDeviceNV || !bridge->wglDXRegisterObjectNV ||
        !bridge->wglDXUnregisterObjectNV || !bridge->wglDXLockObjectsNV || !bridge->wglDXUnlockObjectsNV)
    {
        SetError(bridge, "WGL_NV_DX_interop is not available.");
        return false;
    }

    return true;
}

bool CreateD3DDevice(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    if (!CheckHr(bridge, Direct3DCreate9Ex(D3D_SDK_VERSION, &bridge->Direct3D), "Direct3DCreate9Ex failed."))
    {
        return false;
    }

    D3DPRESENT_PARAMETERS presentParameters = {};
    presentParameters.Windowed = TRUE;
    presentParameters.SwapEffect = D3DSWAPEFFECT_DISCARD;
    presentParameters.hDeviceWindow = GetDesktopWindow();
    presentParameters.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
    presentParameters.BackBufferFormat = D3DFMT_A8R8G8B8;
    presentParameters.BackBufferWidth = 1;
    presentParameters.BackBufferHeight = 1;

    return CheckHr(
        bridge,
        bridge->Direct3D->CreateDeviceEx(
            D3DADAPTER_DEFAULT,
            D3DDEVTYPE_HAL,
            GetDesktopWindow(),
            D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
            &presentParameters,
            nullptr,
            &bridge->D3DDevice),
        "IDirect3D9Ex::CreateDeviceEx failed.");
}

void ReleaseInteropResource(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    if (bridge->DxInteropObject)
    {
        bridge->wglDXUnregisterObjectNV(bridge->DxInteropDevice, bridge->DxInteropObject);
        bridge->DxInteropObject = nullptr;
    }

    if (bridge->Framebuffer)
    {
        bridge->glDeleteFramebuffers(1, &bridge->Framebuffer);
        bridge->Framebuffer = 0;
    }

    if (bridge->GlTexture)
    {
        glDeleteTextures(1, &bridge->GlTexture);
        bridge->GlTexture = 0;
    }

    ReleaseCom(bridge->Surface);
    ReleaseCom(bridge->Texture);
}

bool CreateInteropResource(VtkWpfOpenGLD3DImageProbeBridge* bridge, int width, int height)
{
    ReleaseInteropResource(bridge);

    bridge->Width = std::max(1, width);
    bridge->Height = std::max(1, height);

    HANDLE shareHandle = nullptr;
    if (!CheckHr(
            bridge,
            bridge->D3DDevice->CreateTexture(
                bridge->Width,
                bridge->Height,
                1,
                D3DUSAGE_RENDERTARGET,
                D3DFMT_A8R8G8B8,
                D3DPOOL_DEFAULT,
                &bridge->Texture,
                &shareHandle),
            "IDirect3DDevice9Ex::CreateTexture failed."))
    {
        return false;
    }

    if (bridge->wglDXSetResourceShareHandleNV)
    {
        bridge->wglDXSetResourceShareHandleNV(bridge->Texture, shareHandle);
    }

    if (!CheckHr(bridge, bridge->Texture->GetSurfaceLevel(0, &bridge->Surface), "IDirect3DTexture9::GetSurfaceLevel failed."))
    {
        return false;
    }

    glGenTextures(1, &bridge->GlTexture);
    glBindTexture(GL_TEXTURE_2D, bridge->GlTexture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glBindTexture(GL_TEXTURE_2D, 0);

    bridge->DxInteropObject = bridge->wglDXRegisterObjectNV(
        bridge->DxInteropDevice,
        bridge->Texture,
        bridge->GlTexture,
        GL_TEXTURE_2D,
        WGL_ACCESS_WRITE_DISCARD_NV);
    if (!bridge->DxInteropObject)
    {
        SetError(bridge, "wglDXRegisterObjectNV failed.");
        return false;
    }

    bridge->glGenFramebuffers(1, &bridge->Framebuffer);
    return true;
}

void ReleaseAll(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    if (!bridge)
    {
        return;
    }

    if (bridge->GlContext)
    {
        wglMakeCurrent(bridge->DeviceContext, bridge->GlContext);
    }

    ReleaseInteropResource(bridge);

    if (bridge->DxInteropDevice)
    {
        bridge->wglDXCloseDeviceNV(bridge->DxInteropDevice);
        bridge->DxInteropDevice = nullptr;
    }

    ReleaseCom(bridge->D3DDevice);
    ReleaseCom(bridge->Direct3D);

    if (bridge->GlContext)
    {
        wglMakeCurrent(nullptr, nullptr);
        wglDeleteContext(bridge->GlContext);
        bridge->GlContext = nullptr;
    }

    if (bridge->DeviceContext && bridge->Window)
    {
        ReleaseDC(bridge->Window, bridge->DeviceContext);
        bridge->DeviceContext = nullptr;
    }

    if (bridge->Window)
    {
        DestroyWindow(bridge->Window);
        bridge->Window = nullptr;
    }
}
}

VTKSHARP_API VtkWpfOpenGLD3DImageProbeBridge* VtkWpfOpenGLD3DImageProbeBridge_New()
{
    auto* bridge = new VtkWpfOpenGLD3DImageProbeBridge();

    if (!CreateHiddenOpenGLContext(bridge) ||
        !LoadOpenGLExtensions(bridge) ||
        !CreateD3DDevice(bridge))
    {
        ReleaseAll(bridge);
        delete bridge;
        return nullptr;
    }

    bridge->DxInteropDevice = bridge->wglDXOpenDeviceNV(bridge->D3DDevice);
    if (!bridge->DxInteropDevice)
    {
        SetError(bridge, "wglDXOpenDeviceNV failed.");
        ReleaseAll(bridge);
        delete bridge;
        return nullptr;
    }

    if (!CreateInteropResource(bridge, bridge->Width, bridge->Height))
    {
        ReleaseAll(bridge);
        delete bridge;
        return nullptr;
    }

    return bridge;
}

VTKSHARP_API void VtkWpfOpenGLD3DImageProbeBridge_Delete(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    ReleaseAll(bridge);
    delete bridge;
}

VTKSHARP_API void VtkWpfOpenGLD3DImageProbeBridge_SetSize(VtkWpfOpenGLD3DImageProbeBridge* bridge, int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (bridge->Surface && bridge->Width == clampedWidth && bridge->Height == clampedHeight)
    {
        return;
    }

    CreateInteropResource(bridge, clampedWidth, clampedHeight);
}

VTKSHARP_API void VtkWpfOpenGLD3DImageProbeBridge_Render(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    if (!bridge || !bridge->DxInteropObject)
    {
        return;
    }

    wglMakeCurrent(bridge->DeviceContext, bridge->GlContext);

    HANDLE object = bridge->DxInteropObject;
    if (!bridge->wglDXLockObjectsNV(bridge->DxInteropDevice, 1, &object))
    {
        SetError(bridge, "wglDXLockObjectsNV failed.");
        return;
    }

    bridge->glBindFramebuffer(GL_FRAMEBUFFER, bridge->Framebuffer);
    bridge->glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, bridge->GlTexture, 0);

    if (bridge->glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE)
    {
        const float t = static_cast<float>(bridge->FrameIndex++ % 360) * 0.01745329252f;
        const float r = 0.35f + 0.3f * std::sin(t);
        const float g = 0.35f + 0.3f * std::sin(t + 2.0943951f);
        const float b = 0.35f + 0.3f * std::sin(t + 4.1887902f);

        glViewport(0, 0, bridge->Width, bridge->Height);
        glClearColor(r, g, b, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT);
        glFinish();
    }

    bridge->glBindFramebuffer(GL_FRAMEBUFFER, 0);
    bridge->wglDXUnlockObjectsNV(bridge->DxInteropDevice, 1, &object);
}

VTKSHARP_API IDirect3DSurface9* VtkWpfOpenGLD3DImageProbeBridge_GetBackBuffer(VtkWpfOpenGLD3DImageProbeBridge* bridge)
{
    return bridge ? bridge->Surface : nullptr;
}

VTKSHARP_API const char* VtkWpfOpenGLD3DImageProbeBridge_GetLastError()
{
    return LastProbeError;
}
