#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>

#include "WglContext.h"

namespace
{
    LRESULT CALLBACK RenderTargetWindowProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
    {
        return ::DefWindowProc(hwnd, message, wparam, lparam);
    }
}

bool WglContext::CreateHiddenWindowContext()
{
    const wchar_t* className = L"VtkSharpOpenGlD3DImageRenderTargetWindow";

    WNDCLASSW windowClass = {};
    windowClass.lpfnWndProc = RenderTargetWindowProc;
    windowClass.hInstance = ::GetModuleHandleW(nullptr);
    windowClass.lpszClassName = className;
    ::RegisterClassW(&windowClass);

    this->m_window = ::CreateWindowExW(0, className, L"VtkSharp OpenGL D3DImage Render Target",
                                       WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1, 1, nullptr, nullptr,
                                       ::GetModuleHandleW(nullptr), nullptr);
    if (!this->m_window)
    {
        return false;
    }

    this->m_deviceContext = ::GetDC(this->m_window);
    if (!this->m_deviceContext)
    {
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

    const int format = ::ChoosePixelFormat(this->m_deviceContext, &pixelFormat);
    if (format == 0 || !::SetPixelFormat(this->m_deviceContext, format, &pixelFormat))
    {
        return false;
    }

    this->m_glContext = ::wglCreateContext(this->m_deviceContext);
    if (!this->m_glContext || !::wglMakeCurrent(this->m_deviceContext, this->m_glContext))
    {
        return false;
    }

    this->m_openGL32Library = ::LoadLibraryW(L"opengl32.dll");
    return this->m_openGL32Library != nullptr;
}

void WglContext::Release()
{
    if (this->m_glContext)
    {
        ::wglMakeCurrent(nullptr, nullptr);
        ::wglDeleteContext(this->m_glContext);
        this->m_glContext = nullptr;
    }

    if (this->m_openGL32Library)
    {
        ::FreeLibrary(this->m_openGL32Library);
        this->m_openGL32Library = nullptr;
    }

    if (this->m_deviceContext && this->m_window)
    {
        ::ReleaseDC(this->m_window, this->m_deviceContext);
        this->m_deviceContext = nullptr;
    }

    if (this->m_window)
    {
        ::DestroyWindow(this->m_window);
        this->m_window = nullptr;
    }
}

void WglContext::MakeCurrent() const
{
    ::wglMakeCurrent(this->m_deviceContext, this->m_glContext);
}

bool WglContext::IsCurrent() const
{
    return ::wglGetCurrentContext() == this->m_glContext;
}

vtkOpenGLRenderWindow::VTKOpenGLAPIProc WglContext::LoadSymbol(const char* name) const
{
    auto* proc = reinterpret_cast<void*>(::wglGetProcAddress(name));

    if (proc == nullptr ||
        proc == reinterpret_cast<void*>(1) ||
        proc == reinterpret_cast<void*>(2) ||
        proc == reinterpret_cast<void*>(3) ||
        proc == reinterpret_cast<void*>(-1))
    {
        proc = reinterpret_cast<void*>(::GetProcAddress(this->m_openGL32Library, name));
    }

    return reinterpret_cast<vtkOpenGLRenderWindow::VTKOpenGLAPIProc>(proc);
}
