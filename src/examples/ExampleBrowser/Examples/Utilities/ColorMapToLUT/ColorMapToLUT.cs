using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColorMapToLUT", "Utilities",
    Description = "Builds a discretized color map from control points and applies it to a cone.",
    SourceFiles = new[] { "Examples/Utilities/ColorMapToLUT/ColorMapToLUT.cs" })]
internal sealed class ColorMapToLUT : ISmokeExample
{
    private static readonly (double Position, double R, double G, double B)[] FastColors =
    {
        (0.0, 0.0564, 0.0564, 0.47),
        (0.1715922394, 0.243, 0.46035, 0.81),
        (0.2984914818, 0.3568143827, 0.7450246485, 0.9543677029),
        (0.4321287371, 0.6882, 0.93, 0.91791),
        (0.5, 0.8994959551, 0.9446463943, 0.7686567143),
        (0.5882260353, 0.9571079774, 0.8338185109, 0.5089156299),
        (0.7061412606, 0.927520759, 0.6214389092, 0.3153570584),
        (0.8476395309, 0.8, 0.352, 0.16),
        (1.0, 0.59, 0.0767, 0.119475)
    };

    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Utilities/ColorMapToLUT/
        using var cone = vtkConeSource.New();
        cone.SetResolution(48);
        cone.SetHeight(1);
        using var elevation = vtkElevationFilter.New();
        elevation.SetInputConnection(cone.GetOutputPort());
        elevation.SetLowPoint(0, -0.5, 0);
        elevation.SetHighPoint(0, 0.5, 0);
        using var colorMap = vtkDiscretizableColorTransferFunction.New();
        colorMap.SetColorSpaceToLab();
        colorMap.SetScaleToLinear();
        colorMap.SetNumberOfValues(FastColors.Length);
        colorMap.SetNanColor(0, 1, 0);
        colorMap.SetDiscretize(true);
        foreach (var (position, r, g, b) in FastColors)
            colorMap.AddRGBPoint(position, r, g, b);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(elevation.GetOutputPort());
        mapper.SetLookupTable(colorMap);
        mapper.SetScalarRange(0, 1);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("ColorMapToLUT");
        window.SetSize(800, 500);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ColorMapToLUT", screenshotPath);
    }
}
