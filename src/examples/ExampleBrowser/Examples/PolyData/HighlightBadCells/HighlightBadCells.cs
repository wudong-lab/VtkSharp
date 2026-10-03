using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("HighlightBadCells", "PolyData", Description = "Selects small-area sphere triangles and overlays them as red wireframe.", SourceFiles = new[] { "Examples/PolyData/HighlightBadCells/HighlightBadCells.cs" })]
internal sealed class HighlightBadCells : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var sphere = vtkSphereSource.New();
        using var triangles = vtkTriangleFilter.New();
        triangles.SetInputConnection(sphere.GetOutputPort());
        using var quality = vtkMeshQuality.New();
        quality.SetInputConnection(triangles.GetOutputPort());
        quality.SetTriangleQualityMeasureToArea();
        using var threshold = vtkThreshold.New();
        threshold.SetInputConnection(quality.GetOutputPort());
        threshold.SetLowerThreshold(0.02);
        threshold.SetThresholdFunction(1); // vtkThreshold::THRESHOLD_LOWER
        threshold.SetInputArrayToProcess(0, 0, 0, 1, "Quality");
        using var meshMapper = vtkPolyDataMapper.New();
        meshMapper.SetInputConnection(triangles.GetOutputPort());
        using var meshActor = vtkActor.New();
        meshActor.SetMapper(meshMapper);
        meshActor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
        using var badMapper = vtkDataSetMapper.New();
        badMapper.SetInputConnection(threshold.GetOutputPort());
        using var badActor = vtkActor.New();
        badActor.SetMapper(badMapper);
        badActor.GetProperty().SetRepresentationToWireframe();
        badActor.GetProperty().SetLineWidth(3);
        badActor.GetProperty().SetColor(colors.GetColor3d("Red"));
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("HighlightBadCells");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(meshActor);
        renderer.AddActor(badActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "HighlightBadCells", screenshotPath);
    }
}
