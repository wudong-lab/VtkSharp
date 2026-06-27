using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ClientData", "Interaction",
    Description = "Demonstrates passing client data to a keypress observer callback on a sphere.",
    SourceFiles = new[] { "Examples/Interaction/ClientData/ClientData.cs" })]
internal class ClientData : IExample
{
    public void Run()
    {
        using var colors = vtkNamedColors.New();

        // Create a sphere
        using var sphereSource = vtkSphereSource.New();
        sphereSource.SetCenter(0.0, 0.0, 0.0);
        sphereSource.SetRadius(5.0);
        sphereSource.Update();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphereSource.GetOutputPort());

        // Create an actor
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        var gold = colors.GetColor3d("Goldenrod");
        actor.GetProperty().SetColor(gold.R, gold.G, gold.B);

        // A renderer and render window
        using var renderer = vtkRenderer.New();
        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);

        // An interactor
        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        // Allow the observer to access the sphereSource
        renderWindowInteractor.AddObserver(
            vtkCommand.KeyPressEvent,
            KeypressCallbackFunction,
            clientData: sphereSource);

        renderer.AddActor(actor);
        var gray = colors.GetColor3d("SlateGray");
        renderer.SetBackground(gray.R, gray.G, gray.B);

        renderWindow.SetWindowName("ClientData");
        renderWindow.Render();
        renderWindowInteractor.Start();
    }

    private static void KeypressCallbackFunction(vtkObject caller, uint eventId, object? clientData, nint callData)
    {
        // Prove that we can access the sphere source
        var sphereSource = (vtkSphereSource)clientData!;
        Debug.WriteLine($"Radius is {sphereSource.GetRadius()}");
    }
}
