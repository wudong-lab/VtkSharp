#include <vtkRenderWindow.h>

extern "C" __declspec(dllexport) vtkRenderWindow* VtkSharpRendering_NewRenderWindow()
{
    return vtkRenderWindow::New();
}
