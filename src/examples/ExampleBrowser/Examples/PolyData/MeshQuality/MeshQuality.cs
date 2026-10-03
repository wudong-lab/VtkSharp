using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("MeshQuality", "PolyData", Description = "Computes triangle areas and maps them to a color scale.", SourceFiles = new[] { "Examples/PolyData/MeshQuality/MeshQuality.cs" })]
internal sealed class MeshQuality : ISmokeExample
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
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(quality.GetOutputPort());
        mapper.SetScalarModeToUseCellFieldData();
        mapper.SelectColorArray("Quality");
        mapper.SetScalarRange(0.0, 0.07);
        mapper.SetScalarModeToUseCellData();
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("MeshQuality");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "MeshQuality", screenshotPath);
    }
}
