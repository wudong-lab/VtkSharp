using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Cone", "GeometricObjects",
    Description = "Creates a cone with custom height/radius/resolution and renders it.",
    SourceFiles = new[] { "Examples/GeometricObjects/Cone/Cone.cs" })]
internal class Cone : IExample
{
    public void Run()
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
        Debug.WriteLine("Cone example running. Close the window to exit.");
        interactor.Start();
    }
}
