using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of VTK/Examples/GeometricObjects/Cxx/CylinderExample.cxx
// Creates a cylinder, colors it tomato, rotates it, and renders with a dark blue background.
// vtkNamedColors is not bound; color values are inlined directly.
//   Tomato   = (1.0, 0.388, 0.278)
//   BkgColor = (26/255 ≈ 0.102, 51/255 = 0.2, 102/255 = 0.4)

internal class Cylinder
{
    static void Main()
    {
        using var cylinder = vtkCylinderSource.New();
        cylinder.SetResolution(8);

        using var cylinderMapper = vtkPolyDataMapper.New();
        cylinderMapper.SetInputConnection(cylinder.GetOutputPort());

        using var cylinderActor = vtkActor.New();
        cylinderActor.SetMapper(cylinderMapper);
        cylinderActor.GetProperty().SetColor(1.0, 0.388, 0.278);
        cylinderActor.RotateX(30.0);
        cylinderActor.RotateY(-45.0);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(cylinderActor);
        renderer.SetBackground(0.102, 0.2, 0.4);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Zoom(1.5);

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.SetSize(300, 300);
        renderWindow.AddRenderer(renderer);
        renderWindow.SetWindowName("Cylinder");

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderWindow.Render();
        Console.WriteLine("Cylinder example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
