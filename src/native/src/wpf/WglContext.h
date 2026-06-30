#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>
#include <vtkOpenGLRenderWindow.h>

class WglContext
{
public:
    bool CreateHiddenWindowContext();
    void Release();

    void MakeCurrent() const;
    bool IsCurrent() const;

    vtkOpenGLRenderWindow::VTKOpenGLAPIProc LoadSymbol(const char* name) const;

    HDC DeviceContext() const { return this->m_deviceContext; }

private:
    HWND m_window = nullptr;
    HDC m_deviceContext = nullptr;
    HGLRC m_glContext = nullptr;
    HMODULE m_openGL32Library = nullptr;
};
