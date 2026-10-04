using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("AreaPicking", "Picking",
    Description = "Selects props inside a drag rectangle with vtkAreaPicker.",
    SourceFiles = new[] { "Examples/Picking/AreaPicking/AreaPicking.cs" })]
internal sealed class AreaPicking : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        sphere.SetCenter(-0.8, 0, 0);
        using var sphereMapper = vtkPolyDataMapper.New();
        sphereMapper.SetInputConnection(sphere.GetOutputPort());
        using var sphereActor = vtkActor.New();
        sphereActor.SetMapper(sphereMapper);
        sphereActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
        using var cone = vtkConeSource.New();
        cone.SetCenter(0.8, 0, 0);
        using var coneMapper = vtkPolyDataMapper.New();
        coneMapper.SetInputConnection(cone.GetOutputPort());
        using var coneActor = vtkActor.New();
        coneActor.SetMapper(coneMapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(sphereActor);
        renderer.AddActor(coneActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("AreaPicking");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleRubberBandPick.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkAreaPicker.New();
        var start = (X: 0, Y: 0);
        using var press = interactor.AddLeftButtonPressEventObserver(e => start = (e.X, e.Y));
        using var release = interactor.AddLeftButtonReleaseEventObserver(e =>
        {
            picker.AreaPick(start.X, start.Y, e.X, e.Y, renderer);
            using var props = picker.GetProp3Ds();
            Debug.WriteLine($"Props in rectangle: {props.GetNumberOfItems()}");
        });

        ExampleRenderSupport.Finish(window, interactor, "AreaPicking", screenshotPath);
    }
}
