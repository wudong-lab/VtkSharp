using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of VTK/Examples/GeometricObjects/Cxx/Arrow.cxx
// Creates an arrow source and renders it on a midnight blue background.
// vtkNamedColors not bound; MidnightBlue = (25/255 ≈ 0.098, 25/255 ≈ 0.098, 112/255 ≈ 0.439).
// The duplicate SetWindowName call in the original is omitted (appears to be a copy-paste artifact).

internal class Arrow
{
    static void Main()
    {
        using var arrowSource = vtkArrowSource.New();
        arrowSource.Update();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(arrowSource.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(0.098, 0.098, 0.439);

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.SetWindowName("Arrow");
        renderWindow.AddRenderer(renderer);

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderWindow.Render();
        Console.WriteLine("Arrow example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
