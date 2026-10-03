using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Cutter", "VisualizationAlgorithms", Description = "Cuts a cube with a plane and overlays the resulting section.", SourceFiles = new[] { "Examples/VisualizationAlgorithms/Cutter/Cutter.cs" })]
internal sealed class Cutter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var cube = vtkCubeSource.New();
        cube.SetXLength(40); cube.SetYLength(30); cube.SetZLength(20);
        using var plane = vtkPlane.New(); plane.SetOrigin(10, 0, 0); plane.SetNormal(1, 0, 0);
        using var cutter = vtkCutter.New(); cutter.SetCutFunction(plane); cutter.SetInputConnection(cube.GetOutputPort());
        using var sectionMapper = vtkPolyDataMapper.New(); sectionMapper.SetInputConnection(cutter.GetOutputPort());
        using var sectionActor = vtkActor.New(); sectionActor.SetMapper(sectionMapper); sectionActor.GetProperty().SetColor(colors.GetColor3d("Yellow")); sectionActor.GetProperty().SetLineWidth(2);
        using var cubeMapper = vtkPolyDataMapper.New(); cubeMapper.SetInputConnection(cube.GetOutputPort());
        using var cubeActor = vtkActor.New(); cubeActor.SetMapper(cubeMapper); cubeActor.GetProperty().SetColor(colors.GetColor3d("Aquamarine")); cubeActor.GetProperty().SetOpacity(0.5);
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetSize(600, 600); window.SetWindowName("Cutter");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(sectionActor); renderer.AddActor(cubeActor); renderer.SetBackground(colors.GetColor3d("Silver"));
        renderer.ResetCamera(); renderer.GetActiveCamera().Azimuth(30); renderer.GetActiveCamera().Elevation(30);
        ExampleRenderSupport.Finish(window, interactor, "Cutter", screenshotPath);
    }
}
