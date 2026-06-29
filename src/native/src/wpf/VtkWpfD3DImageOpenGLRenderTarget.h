#pragma once

class vtkRenderWindow;
class vtkRenderer;
struct IDirect3DSurface9;

class VtkWpfD3DImageOpenGLRenderTarget
{
public:
    static VtkWpfD3DImageOpenGLRenderTarget* Create();
    static const char* GetLastError();

    ~VtkWpfD3DImageOpenGLRenderTarget();

    vtkRenderWindow* GetRenderWindow() const;
    vtkRenderer* GetRenderer() const;
    IDirect3DSurface9* GetBackBuffer() const;

    void SetSize(int width, int height);
    void Render();

private:
    VtkWpfD3DImageOpenGLRenderTarget();

    VtkWpfD3DImageOpenGLRenderTarget(const VtkWpfD3DImageOpenGLRenderTarget&) = delete;
    VtkWpfD3DImageOpenGLRenderTarget& operator=(const VtkWpfD3DImageOpenGLRenderTarget&) = delete;

    class Impl;
    Impl* impl_;
};
