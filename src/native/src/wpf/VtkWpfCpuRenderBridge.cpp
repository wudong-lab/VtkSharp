#include "vtksharp_api.h"

#include <vtkImageData.h>
#include <vtkRenderWindow.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>
#include <vtkWindowToImageFilter.h>

#include <algorithm>
#include <cstdint>

namespace
{
struct VtkWpfCpuRenderBridge
{
    vtkSmartPointer<vtkRenderWindow> RenderWindow;
    vtkSmartPointer<vtkRenderer> Renderer;
    vtkSmartPointer<vtkWindowToImageFilter> Capture;

    int Width = 1;
    int Height = 1;
};
}

VTKSHARP_API VtkWpfCpuRenderBridge* VtkWpfCpuRenderBridge_New()
{
    auto* bridge = new VtkWpfCpuRenderBridge();
    bridge->RenderWindow = vtkSmartPointer<vtkRenderWindow>::New();
    bridge->Renderer = vtkSmartPointer<vtkRenderer>::New();
    bridge->Capture = vtkSmartPointer<vtkWindowToImageFilter>::New();

    bridge->RenderWindow->OffScreenRenderingOn();
    bridge->RenderWindow->SetSize(bridge->Width, bridge->Height);
    bridge->RenderWindow->AddRenderer(bridge->Renderer);

    bridge->Capture->SetInput(bridge->RenderWindow);
    bridge->Capture->SetInputBufferTypeToRGBA();
    bridge->Capture->ReadFrontBufferOff();
    bridge->Capture->ShouldRerenderOff();

    return bridge;
}

VTKSHARP_API void VtkWpfCpuRenderBridge_Delete(VtkWpfCpuRenderBridge* bridge)
{
    delete bridge;
}

VTKSHARP_API vtkRenderWindow* VtkWpfCpuRenderBridge_GetRenderWindow(VtkWpfCpuRenderBridge* bridge)
{
    return bridge->RenderWindow;
}

VTKSHARP_API vtkRenderer* VtkWpfCpuRenderBridge_GetRenderer(VtkWpfCpuRenderBridge* bridge)
{
    return bridge->Renderer;
}

VTKSHARP_API void VtkWpfCpuRenderBridge_SetSize(VtkWpfCpuRenderBridge* bridge, int width, int height)
{
    bridge->Width = std::max(1, width);
    bridge->Height = std::max(1, height);
    bridge->RenderWindow->SetSize(bridge->Width, bridge->Height);
}

VTKSHARP_API void VtkWpfCpuRenderBridge_Render(VtkWpfCpuRenderBridge* bridge)
{
    bridge->RenderWindow->Render();
}

VTKSHARP_API int VtkWpfCpuRenderBridge_CopyBgra(
    VtkWpfCpuRenderBridge* bridge,
    std::uint8_t* destination,
    int destinationLength,
    int* width,
    int* height)
{
    if (!bridge || !destination || destinationLength <= 0 || !width || !height)
    {
        return 0;
    }

    bridge->Capture->Modified();
    bridge->Capture->Update();

    vtkImageData* image = bridge->Capture->GetOutput();
    int dimensions[3] = {0, 0, 0};
    image->GetDimensions(dimensions);

    const int imageWidth = dimensions[0];
    const int imageHeight = dimensions[1];
    constexpr int componentCount = 4;
    const int requiredLength = imageWidth * imageHeight * componentCount;

    *width = imageWidth;
    *height = imageHeight;

    if (imageWidth <= 0 || imageHeight <= 0 || destinationLength < requiredLength)
    {
        return 0;
    }

    auto* source = static_cast<std::uint8_t*>(image->GetScalarPointer());
    if (!source)
    {
        return 0;
    }

    for (int y = 0; y < imageHeight; ++y)
    {
        const int sourceY = imageHeight - 1 - y;
        const std::uint8_t* sourceRow = source + sourceY * imageWidth * componentCount;
        std::uint8_t* destinationRow = destination + y * imageWidth * componentCount;

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

    return 1;
}
