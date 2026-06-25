using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of VTK/Examples/GeometricObjects/Cxx/CylinderExample.cxx
// Creates a cylinder and renders it with a tomato-colored actor.

internal class Cylinder
{
    static void Main()
    {
        using var cylinder = vtkCylinderSource.New();
        cylinder.SetResolution(8);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cylinder.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(1.0, 0.388, 0.278);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(0.1, 0.2, 0.4);

        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(300, 300);

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        window.Render();
        Console.WriteLine("Cylinder example running. Close the window to exit.");
        interactor.Start();
    }
}
