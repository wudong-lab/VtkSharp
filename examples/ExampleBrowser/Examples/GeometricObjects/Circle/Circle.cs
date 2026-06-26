using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Circle", "GeometricObjects",
    Description = "Creates a circle using vtkRegularPolygonSource and renders it in cornsilk color on a dark green background.",
    SourceFiles = new[] { "Examples/GeometricObjects/Circle/Circle.cs" })]
internal class Circle : IExample
{
    public void Run()
    {
        using var colors = vtkNamedColors.New();

        using var polygonSource = vtkRegularPolygonSource.New();
        polygonSource.GeneratePolygonOff();
        polygonSource.SetNumberOfSides(50);
        polygonSource.SetRadius(5.0);
        polygonSource.SetCenter(0.0, 0.0, 0.0);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(polygonSource.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        var cornsilk = colors.GetColor3d("Cornsilk");
        actor.GetProperty().SetColor(cornsilk.R, cornsilk.G, cornsilk.B);

        var darkGreen = colors.GetColor3d("DarkGreen");

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(darkGreen.R, darkGreen.G, darkGreen.B);

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderWindow.SetWindowName("Circle");
        renderWindow.Render();
        Console.WriteLine("Circle example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
