using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("InteractorStyleTerrain", "Interaction",
    Description = "Demonstrates vtkInteractorStyleTerrain with sphere rendering.",
    SourceFiles = new[] { "Examples/Interaction/InteractorStyleTerrain/InteractorStyleTerrain.cs" })]
internal class InteractorStyleTerrain : IExample
{
    public void Run()
    {
        using var colors = vtkNamedColors.New();

        using var sphereSource = vtkSphereSource.New();
        sphereSource.Update();

        // Create a mapper and actor
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphereSource.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        var mistyRose = colors.GetColor3d("MistyRose");
        actor.GetProperty().SetColor(mistyRose.R, mistyRose.G, mistyRose.B);

        // Create a renderer, render window, and interactor
        using var renderer = vtkRenderer.New();
        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);
        renderWindow.SetWindowName("InteractorStyleTerrain");

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        using var style = vtkInteractorStyleTerrain.New();
        renderWindowInteractor.SetInteractorStyle(style);

        // Add the actor to the scene
        renderer.AddActor(actor);
        var slateGray = colors.GetColor3d("SlateGray");
        renderer.SetBackground(slateGray.R, slateGray.G, slateGray.B);

        // Render and interact
        renderWindow.Render();
        renderer.GetActiveCamera().Azimuth(45);
        renderer.GetActiveCamera().Elevation(30);
        renderWindow.Render();
        Debug.WriteLine("InteractorStyleTerrain example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
