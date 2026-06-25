using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of VTK/Examples/GeometricObjects/Cxx/Cone.cxx
// Creates a cone with custom height/radius/resolution and renders it.

internal class Cone
{
    static void Main()
    {
        using var cone = vtkConeSource.New();
        cone.SetHeight(3.0);
        cone.SetRadius(1.0);
        cone.SetResolution(10);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);

        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(800, 600);

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        window.Render();
        Console.WriteLine("Cone example running. Close the window to exit.");
        interactor.Start();
    }
}
