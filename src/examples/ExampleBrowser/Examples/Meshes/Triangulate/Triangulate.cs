using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Triangulate", "Meshes",
    SourceFiles = new[] { "Examples/Meshes/Triangulate/Triangulate.cs" })]
internal sealed class Triangulate : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/Meshes/Triangulate/
        using var colors = vtkNamedColors.New();

        using var polygonSource = vtkRegularPolygonSource.New();
        polygonSource.Update();

        using var triangleFilter = vtkTriangleFilter.New();
        triangleFilter.SetInputConnection(polygonSource.GetOutputPort());
        triangleFilter.Update();

        using var inputMapper = vtkPolyDataMapper.New();
        inputMapper.SetInputConnection(polygonSource.GetOutputPort());
        using var inputActor = vtkActor.New();
        inputActor.SetMapper(inputMapper);
        inputActor.GetProperty().SetRepresentationToWireframe();
        inputActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var triangleMapper = vtkPolyDataMapper.New();
        triangleMapper.SetInputConnection(triangleFilter.GetOutputPort());
        using var triangleActor = vtkActor.New();
        triangleActor.SetMapper(triangleMapper);
        triangleActor.GetProperty().SetRepresentationToWireframe();
        triangleActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));

        using var leftRenderer = vtkRenderer.New();
        leftRenderer.SetViewport(0.0, 0.0, 0.5, 1.0);
        leftRenderer.SetBackground(colors.GetColor3d("SaddleBrown"));

        using var rightRenderer = vtkRenderer.New();
        rightRenderer.SetViewport(0.5, 0.0, 1.0, 1.0);
        rightRenderer.SetBackground(colors.GetColor3d("DarkSlateGray"));

        leftRenderer.AddActor(inputActor);
        rightRenderer.AddActor(triangleActor);
        leftRenderer.ResetCamera();
        rightRenderer.ResetCamera();

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.SetSize(600, 300);
        renderWindow.SetWindowName("Triangulate");
        renderWindow.AddRenderer(leftRenderer);
        renderWindow.AddRenderer(rightRenderer);

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(renderWindow);

        renderWindow.Render();
        if (screenshotPath is not null)
        {
            using var image = renderWindow.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        Debug.WriteLine("Triangulate example running. Close the window to exit.");
        interactor.Start();
    }
}
