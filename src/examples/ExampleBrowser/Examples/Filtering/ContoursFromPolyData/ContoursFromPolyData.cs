using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ContoursFromPolyData", "Filtering",
    Description = "Cuts a polydata surface with a plane at multiple contour values.",
    SourceFiles = new[] { "Examples/Filtering/ContoursFromPolyData/ContoursFromPolyData.cs" })]
internal sealed class ContoursFromPolyData : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/Filtering/ContoursFromPolyData/
        using var sphere = vtkSphereSource.New();
        sphere.SetThetaResolution(30);
        sphere.SetPhiResolution(15);
        using var plane = vtkPlane.New();
        plane.SetOrigin(0, 0, 0);
        plane.SetNormal(1, 1, 1);
        using var cutter = vtkCutter.New();
        cutter.SetInputConnection(sphere.GetOutputPort());
        cutter.SetCutFunction(plane);
        cutter.GenerateValues(20, -1.73, 1.73);

        using var contourMapper = vtkPolyDataMapper.New();
        contourMapper.SetInputConnection(cutter.GetOutputPort());
        contourMapper.ScalarVisibilityOff();
        using var contourActor = vtkActor.New();
        contourActor.SetMapper(contourMapper);
        using var sphereMapper = vtkPolyDataMapper.New();
        sphereMapper.SetInputConnection(sphere.GetOutputPort());
        using var sphereActor = vtkActor.New();
        sphereActor.SetMapper(sphereMapper);

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        renderer.AddActor(sphereActor);
        renderer.AddActor(contourActor);
        renderer.SetBackground(colors.GetColor3d("SlateGrey"));
        var deepPink = colors.GetColor3d("DeepPink");
        contourActor.GetProperty().SetColor(deepPink.R, deepPink.G, deepPink.B);
        contourActor.GetProperty().SetLineWidth(3);
        var bisque = colors.GetColor3d("Bisque");
        sphereActor.GetProperty().SetColor(bisque.R, bisque.G, bisque.B);

        using var window = vtkRenderWindow.New();
        window.SetWindowName("ContoursFromPolyData");
        window.SetSize(600, 600);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.ResetCamera();
        window.Render();
        if (screenshotPath is not null)
        {
            using var image = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        interactor.Start();
    }
}
