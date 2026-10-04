using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("CallBack", "Interaction",
    Description = "Reports camera position and focal point after interaction ends.",
    SourceFiles = new[] { "Examples/Interaction/CallBack/CallBack.cs" })]
internal sealed class CallBack : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(sphere.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("CallBack");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var observer = interactor.AddObserver(vtkCommand.EndInteractionEvent, (_, _, _, _) =>
        {
            Span<double> position = stackalloc double[3];
            Span<double> focalPoint = stackalloc double[3];
            var camera = renderer.GetActiveCamera();
            camera.GetPosition(position);
            camera.GetFocalPoint(focalPoint);
            Debug.WriteLine($"Camera position: ({position[0]}, {position[1]}, {position[2]})");
            Debug.WriteLine($"Camera focal point: ({focalPoint[0]}, {focalPoint[1]}, {focalPoint[2]})");
        });

        ExampleRenderSupport.Finish(window, interactor, "CallBack", screenshotPath);
    }
}
