using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColoredSphere", "Rendering",
    Description = "Colors a sphere by elevation scalars.",
    SourceFiles = new[] { "Examples/Rendering/ColoredSphere/ColoredSphere.cs" })]
internal sealed class ColoredSphere : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Rendering/ColoredSphere/
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(48);
        sphere.SetPhiResolution(32);
        using var elevation = vtkElevationFilter.New();
        elevation.SetInputConnection(sphere.GetOutputPort());
        elevation.SetLowPoint(0, 0, -1);
        elevation.SetHighPoint(0, 0, 1);
        elevation.SetScalarRange(0, 1);
        using var lut = vtkLookupTable.New();
        lut.SetTableRange(0, 1);
        lut.Build();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(lut);
        mapper.SetScalarRange(0, 1);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ColoredSphere");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ColoredSphere", screenshotPath);
    }
}
