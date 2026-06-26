using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Arrow", "GeometricObjects",
    Description = "Creates an arrow source and renders it on a midnight blue background.",
    SourceFiles = new[] { "Examples/GeometricObjects/Arrow/Arrow.cs" })]
internal class Arrow : IExample
{
    public void Run()
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
