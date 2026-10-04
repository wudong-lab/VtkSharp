using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("DijkstraGraphGeodesicPath", "PolyData",
    Description = "Finds a shortest path along the edges of a polygonal surface.",
    SourceFiles = new[] { "Examples/PolyData/DijkstraGraphGeodesicPath/DijkstraGraphGeodesicPath.cs" })]
internal sealed class DijkstraGraphGeodesicPath : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/DijkstraGraphGeodesicPath/
        using var sphere = vtkSphereSource.New();
        sphere.Update();
        using var path = vtkDijkstraGraphGeodesicPath.New();
        path.SetInputConnection(sphere.GetOutputPort());
        path.SetStartVertex(0);
        path.SetEndVertex(7);

        using var surfaceMapper = vtkPolyDataMapper.New();
        surfaceMapper.SetInputConnection(sphere.GetOutputPort());
        using var surfaceActor = vtkActor.New();
        surfaceActor.SetMapper(surfaceMapper);
        using var pathMapper = vtkPolyDataMapper.New();
        pathMapper.SetInputConnection(path.GetOutputPort());
        using var pathActor = vtkActor.New();
        pathActor.SetMapper(pathMapper);
        pathActor.GetProperty().SetLineWidth(4);
        using var colors = vtkNamedColors.New();
        var hotPink = colors.GetColor3d("HotPink");
        pathActor.GetProperty().SetColor(hotPink.R, hotPink.G, hotPink.B);
        var mistyRose = colors.GetColor3d("MistyRose");
        surfaceActor.GetProperty().SetColor(mistyRose.R, mistyRose.G, mistyRose.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(surfaceActor);
        renderer.AddActor(pathActor);
        renderer.SetBackground(colors.GetColor3d("MidnightBlue"));
        using var window = vtkRenderWindow.New();
        window.SetWindowName("DijkstraGraphGeodesicPath");
        window.SetSize(640, 480);
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
