using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("SelectAnActor", "Interaction",
    Description = "Identifies whether the cube or sphere was clicked while allowing actors to be dragged.",
    SourceFiles = new[] { "Examples/Interaction/SelectAnActor/SelectAnActor.cs" })]
internal sealed class SelectAnActor : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var cube = vtkCubeSource.New();
        using var cubeMapper = vtkPolyDataMapper.New();
        cubeMapper.SetInputConnection(cube.GetOutputPort());
        using var cubeActor = vtkActor.New();
        cubeActor.SetMapper(cubeMapper);
        cubeActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var sphere = vtkSphereSource.New();
        sphere.SetCenter(2, 0, 0);
        using var sphereMapper = vtkPolyDataMapper.New();
        sphereMapper.SetInputConnection(sphere.GetOutputPort());
        using var sphereActor = vtkActor.New();
        sphereActor.SetMapper(sphereMapper);
        sphereActor.GetProperty().SetColor(colors.GetColor3d("LightGoldenrodYellow"));

        using var renderer = vtkRenderer.New();
        renderer.AddActor(cubeActor);
        renderer.AddActor(sphereActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        renderer.GetActiveCamera().Zoom(0.9);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("SelectAnActor");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballActor.New();
        interactor.SetInteractorStyle(style);
        using var picker = vtkPropPicker.New();
        using var observer = interactor.AddLeftButtonPressEventObserver(e =>
        {
            picker.Pick(e.X, e.Y, 0, renderer);
            var pickedPointer = picker.GetActor().NativePointer;
            if (pickedPointer == cubeActor.NativePointer) Debug.WriteLine("Picked cube.");
            else if (pickedPointer == sphereActor.NativePointer) Debug.WriteLine("Picked sphere.");
        }, priority: -1);

        ExampleRenderSupport.Finish(window, interactor, "SelectAnActor", screenshotPath);
    }
}
