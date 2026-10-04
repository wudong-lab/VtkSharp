using System.Diagnostics;
using System.Runtime.InteropServices;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("RubberBand2DObserver", "Interaction",
    Description = "Reports the rectangle produced by vtkInteractorStyleRubberBand2D.",
    SourceFiles = new[] { "Examples/Interaction/RubberBand2DObserver/RubberBand2DObserver.cs" })]
internal sealed class RubberBand2DObserver : ISmokeExample
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
        window.SetWindowName("RubberBand2DObserver");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleRubberBand2D.New();
        interactor.SetInteractorStyle(style);
        using var observer = style.AddObserver(vtkCommand.SelectionChangedEvent, (_, _, _, callData) =>
        {
            var rectangle = new int[4];
            Marshal.Copy(callData, rectangle, 0, rectangle.Length);
            Debug.WriteLine($"Selection rectangle: ({rectangle[0]}, {rectangle[1]}) - ({rectangle[2]}, {rectangle[3]})");
        });

        ExampleRenderSupport.Finish(window, interactor, "RubberBand2DObserver", screenshotPath);
    }
}
