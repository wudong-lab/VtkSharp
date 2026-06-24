using System;
using VtkSharp;

namespace TestConsole;

internal class Program
{
    static void Main(string[] args)
    {
        var smoke = Array.Exists(args, arg => string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase));

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
        if (!smoke)
            interactor.Start();

        Console.WriteLine(smoke ? "Smoke test completed." : "Done!");
    }
}
