using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("QuadricDecimation", "Meshes", Description = "Reduces a sphere triangle mesh using quadric error metrics.", SourceFiles = new[] { "Examples/Meshes/QuadricDecimation/QuadricDecimation.cs" })]
internal sealed class QuadricDecimation : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var sphere = vtkSphereSource.New(); sphere.SetThetaResolution(48); sphere.SetPhiResolution(32);
        using var decimate = vtkQuadricDecimation.New(); decimate.SetInputConnection(sphere.GetOutputPort()); decimate.SetTargetReduction(0.8);
        using var mapper = vtkPolyDataMapper.New(); mapper.SetInputConnection(decimate.GetOutputPort());
        using var actor = vtkActor.New(); actor.SetMapper(mapper); actor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("QuadricDecimation");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "QuadricDecimation", screenshotPath);
    }
}
