using System;
using VtkSharp;

namespace TestConsole;

internal class Program
{
    static void Main(string[] args)
    {
        using var cone = vtkConeSource.New();

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
        interactor.Start();

        Console.WriteLine("Done!");
    }
}