using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BackgroundGradient", "Visualization",
    Description = "Renders a sphere with a gradient background using named colors.",
    SourceFiles = new[] { "Examples/Visualization/BackgroundGradient/BackgroundGradient.cs" })]
internal class BackgroundGradient : IExample
{
    public void Run()
    {
        using var sphereSource = vtkSphereSource.New();
        sphereSource.Update();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphereSource.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderer.AddActor(actor);

        using var colors = vtkNamedColors.New();

        renderer.GradientBackgroundOn();
        var banana = colors.GetColor3d("Banana");
        renderer.SetBackground(banana.R, banana.G, banana.B);
        var tomato = colors.GetColor3d("Tomato");
        renderer.SetBackground2(tomato.R, tomato.G, tomato.B);

        renderWindow.Render();
        Console.WriteLine("BackgroundGradient example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
