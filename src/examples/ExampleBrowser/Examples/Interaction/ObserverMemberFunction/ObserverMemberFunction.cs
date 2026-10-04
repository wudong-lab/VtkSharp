using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ObserverMemberFunction", "Interaction",
    Description = "Registers an instance method as a managed VTK event observer.",
    SourceFiles = new[] { "Examples/Interaction/ObserverMemberFunction/ObserverMemberFunction.cs" })]
internal sealed class ObserverMemberFunction : ISmokeExample
{
    private sealed class KeyObserver
    {
        public void OnKeyPress(VtkKeyEventArgs e) => Debug.WriteLine($"Observed key: {e.KeySym}");
    }

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
        window.SetWindowName("ObserverMemberFunction");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        var callback = new KeyObserver();
        using var observer = interactor.AddKeyPressEventObserver(callback.OnKeyPress, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "ObserverMemberFunction", screenshotPath);
    }
}
