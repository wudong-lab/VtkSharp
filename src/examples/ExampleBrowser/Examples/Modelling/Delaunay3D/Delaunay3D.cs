using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Delaunay3D", "Modelling", Description = "Tetrahedralizes a generated point cloud and displays the cell mesh.", SourceFiles = new[] { "Examples/Modelling/Delaunay3D/Delaunay3D.cs" })]
internal sealed class Delaunay3D : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var points = vtkPointSource.New(); points.SetNumberOfPoints(100); points.SetRadius(1.0);
        using var delaunay = vtkDelaunay3D.New(); delaunay.SetInputConnection(points.GetOutputPort()); delaunay.SetAlpha(0.0);
        using var mapper = vtkDataSetMapper.New(); mapper.SetInputConnection(delaunay.GetOutputPort());
        using var actor = vtkActor.New(); actor.SetMapper(mapper); actor.GetProperty().EdgeVisibilityOn();
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("Delaunay3D");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "Delaunay3D", screenshotPath);
    }
}
