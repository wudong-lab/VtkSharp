#include "vtksharp_api.h"

#include <vtkImageData.h>
#include <vtkRenderWindow.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>
#include <vtkWindowToImageFilter.h>

#include <algorithm>
#include <cstdint>
#include <d3d9.h>
#include <stdexcept>

namespace
{
struct VtkWpfD3DImageRenderBridge
{
    vtkSmartPointer<vtkRenderWindow> RenderWindow;
    vtkSmartPointer<vtkRenderer> Renderer;
    vtkSmartPointer<vtkWindowToImageFilter> Capture;

    IDirect3D9Ex* Direct3D = nullptr;
    IDirect3DDevice9Ex* Device = nullptr;
    IDirect3DTexture9* Texture = nullptr;
    IDirect3DSurface9* RenderSurface = nullptr;
    IDirect3DSurface9* SystemSurface = nullptr;

    int Width = 1;
    int Height = 1;
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

void ThrowIfFailed(HRESULT hr, const char* message)
{
    if (FAILED(hr))
    {
        throw std::runtime_error(message);
    }
}

void ReleaseD3DResources(VtkWpfD3DImageRenderBridge* bridge)
{
    ReleaseCom(bridge->SystemSurface);
    ReleaseCom(bridge->RenderSurface);
    ReleaseCom(bridge->Texture);
    ReleaseCom(bridge->Device);
    ReleaseCom(bridge->Direct3D);
}

void CreateDevice(VtkWpfD3DImageRenderBridge* bridge)
{
    ThrowIfFailed(Direct3DCreate9Ex(D3D_SDK_VERSION, &bridge->Direct3D), "Direct3DCreate9Ex failed.");

    D3DPRESENT_PARAMETERS presentParameters = {};
    presentParameters.Windowed = TRUE;
    presentParameters.SwapEffect = D3DSWAPEFFECT_DISCARD;
    presentParameters.hDeviceWindow = GetDesktopWindow();
    presentParameters.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
    presentParameters.BackBufferFormat = D3DFMT_A8R8G8B8;
    presentParameters.BackBufferWidth = 1;
    presentParameters.BackBufferHeight = 1;

    ThrowIfFailed(
        bridge->Direct3D->CreateDeviceEx(
            D3DADAPTER_DEFAULT,
            D3DDEVTYPE_HAL,
            GetDesktopWindow(),
            D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
            &presentParameters,
            nullptr,
            &bridge->Device),
        "IDirect3D9Ex::CreateDeviceEx failed.");
}

void CreateSurfaces(VtkWpfD3DImageRenderBridge* bridge, int width, int height)
{
    ReleaseCom(bridge->SystemSurface);
    ReleaseCom(bridge->RenderSurface);
    ReleaseCom(bridge->Texture);

    bridge->Width = std::max(1, width);
    bridge->Height = std::max(1, height);

    ThrowIfFailed(
        bridge->Device->CreateTexture(
            bridge->Width,
            bridge->Height,
            1,
            D3DUSAGE_RENDERTARGET,
            D3DFMT_A8R8G8B8,
            D3DPOOL_DEFAULT,
            &bridge->Texture,
            nullptr),
        "IDirect3DDevice9Ex::CreateTexture failed.");

    ThrowIfFailed(bridge->Texture->GetSurfaceLevel(0, &bridge->RenderSurface), "IDirect3DTexture9::GetSurfaceLevel failed.");

    ThrowIfFailed(
        bridge->Device->CreateOffscreenPlainSurface(
            bridge->Width,
            bridge->Height,
            D3DFMT_A8R8G8B8,
            D3DPOOL_SYSTEMMEM,
            &bridge->SystemSurface,
            nullptr),
        "IDirect3DDevice9Ex::CreateOffscreenPlainSurface failed.");

    bridge->RenderWindow->SetSize(bridge->Width, bridge->Height);
}

void CopyCapturedFrameToSurface(VtkWpfD3DImageRenderBridge* bridge)
{
    bridge->Capture->Modified();
    bridge->Capture->Update();

    vtkImageData* image = bridge->Capture->GetOutput();
    int dimensions[3] = {0, 0, 0};
    image->GetDimensions(dimensions);

    const int imageWidth = dimensions[0];
    const int imageHeight = dimensions[1];
    constexpr int componentCount = 4;

    if (imageWidth <= 0 || imageHeight <= 0 || imageWidth != bridge->Width || imageHeight != bridge->Height)
    {
        return;
    }

    auto* source = static_cast<std::uint8_t*>(image->GetScalarPointer());
    if (!source)
    {
        return;
    }

    D3DLOCKED_RECT locked = {};
    ThrowIfFailed(bridge->SystemSurface->LockRect(&locked, nullptr, 0), "IDirect3DSurface9::LockRect failed.");

    for (int y = 0; y < imageHeight; ++y)
    {
        const int sourceY = imageHeight - 1 - y;
        const std::uint8_t* sourceRow = source + sourceY * imageWidth * componentCount;
        std::uint8_t* destinationRow = static_cast<std::uint8_t*>(locked.pBits) + y * locked.Pitch;

        for (int x = 0; x < imageWidth; ++x)
        {
            const std::uint8_t* sourcePixel = sourceRow + x * componentCount;
            std::uint8_t* destinationPixel = destinationRow + x * componentCount;

            destinationPixel[0] = sourcePixel[2];
            destinationPixel[1] = sourcePixel[1];
            destinationPixel[2] = sourcePixel[0];
            destinationPixel[3] = 255;
        }
    }

    bridge->SystemSurface->UnlockRect();
    ThrowIfFailed(bridge->Device->UpdateSurface(bridge->SystemSurface, nullptr, bridge->RenderSurface, nullptr), "IDirect3DDevice9Ex::UpdateSurface failed.");
}
}

VTKSHARP_API VtkWpfD3DImageRenderBridge* VtkWpfD3DImageRenderBridge_New()
{
    auto* bridge = new VtkWpfD3DImageRenderBridge();

    try
    {
        bridge->RenderWindow = vtkSmartPointer<vtkRenderWindow>::New();
        bridge->Renderer = vtkSmartPointer<vtkRenderer>::New();
        bridge->Capture = vtkSmartPointer<vtkWindowToImageFilter>::New();

        bridge->RenderWindow->OffScreenRenderingOn();
        bridge->RenderWindow->AddRenderer(bridge->Renderer);

        bridge->Capture->SetInput(bridge->RenderWindow);
        bridge->Capture->SetInputBufferTypeToRGBA();
        bridge->Capture->ReadFrontBufferOff();
        bridge->Capture->ShouldRerenderOff();

        CreateDevice(bridge);
        CreateSurfaces(bridge, bridge->Width, bridge->Height);
    }
    catch (...)
    {
        ReleaseD3DResources(bridge);
        delete bridge;
        return nullptr;
    }

    return bridge;
}

VTKSHARP_API void VtkWpfD3DImageRenderBridge_Delete(VtkWpfD3DImageRenderBridge* bridge)
{
    if (!bridge)
    {
        return;
    }

    ReleaseD3DResources(bridge);
    delete bridge;
}

VTKSHARP_API vtkRenderWindow* VtkWpfD3DImageRenderBridge_GetRenderWindow(VtkWpfD3DImageRenderBridge* bridge)
{
    return bridge->RenderWindow;
}

VTKSHARP_API vtkRenderer* VtkWpfD3DImageRenderBridge_GetRenderer(VtkWpfD3DImageRenderBridge* bridge)
{
    return bridge->Renderer;
}

VTKSHARP_API void VtkWpfD3DImageRenderBridge_SetSize(VtkWpfD3DImageRenderBridge* bridge, int width, int height)
{
    const int clampedWidth = std::max(1, width);
    const int clampedHeight = std::max(1, height);

    if (bridge->RenderSurface && bridge->Width == clampedWidth && bridge->Height == clampedHeight)
    {
        return;
    }

    CreateSurfaces(bridge, clampedWidth, clampedHeight);
}

VTKSHARP_API void VtkWpfD3DImageRenderBridge_Render(VtkWpfD3DImageRenderBridge* bridge)
{
    bridge->RenderWindow->Render();
    CopyCapturedFrameToSurface(bridge);
}

VTKSHARP_API IDirect3DSurface9* VtkWpfD3DImageRenderBridge_GetBackBuffer(VtkWpfD3DImageRenderBridge* bridge)
{
    return bridge->RenderSurface;
}
