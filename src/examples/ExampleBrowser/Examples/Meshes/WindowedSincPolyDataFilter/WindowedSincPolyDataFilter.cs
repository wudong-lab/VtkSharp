using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("WindowedSincPolyDataFilter", "Meshes", Description = "Smooths a triangulated sphere surface with a windowed-sinc filter.", SourceFiles = new[] { "Examples/Meshes/WindowedSincPolyDataFilter/WindowedSincPolyDataFilter.cs" })]
internal sealed class WindowedSincPolyDataFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var sphere = vtkSphereSource.New();
        sphere.SetPhiResolution(12); sphere.SetThetaResolution(12);
        using var smooth = vtkWindowedSincPolyDataFilter.New(); smooth.SetInputConnection(sphere.GetOutputPort()); smooth.SetNumberOfIterations(15); smooth.BoundarySmoothingOff();
        using var mapper = vtkPolyDataMapper.New(); mapper.SetInputConnection(smooth.GetOutputPort());
        using var actor = vtkActor.New(); actor.SetMapper(mapper); actor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("WindowedSincPolyDataFilter");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "WindowedSincPolyDataFilter", screenshotPath);
    }
}
