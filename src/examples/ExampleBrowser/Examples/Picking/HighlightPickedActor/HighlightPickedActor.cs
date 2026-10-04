using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("HighlightPickedActor", "Picking",
    Description = "Changes the picked actor to red and enables its edges.",
    SourceFiles = new[] { "Examples/Picking/HighlightPickedActor/HighlightPickedActor.cs" })]
internal sealed class HighlightPickedActor : ISmokeExample
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
        actor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("HighlightPickedActor");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkPropPicker.New();
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            picker.Pick(e.X, e.Y, 0, renderer);
            var picked = picker.GetActor();
            if (picked.NativePointer == 0)
            {
                Debug.WriteLine("No actor picked.");
                return;
            }
            picked.GetProperty().SetColor(colors.GetColor3d("Red"));
            picked.GetProperty().EdgeVisibilityOn();
            window.Render();
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "HighlightPickedActor", screenshotPath);
    }
}
