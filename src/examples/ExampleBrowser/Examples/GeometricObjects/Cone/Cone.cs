using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Cone", "GeometricObjects",
    Description = "Creates a cone with custom height/radius/resolution and renders it.",
    SourceFiles = new[] { "Examples/GeometricObjects/Cone/Cone.cs" })]
internal class Cone : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
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
        if (screenshotPath is not null)
        {
            using var image = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        Debug.WriteLine("Cone example running. Close the window to exit.");
        interactor.Start();
    }
}
