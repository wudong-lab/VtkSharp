using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("BoundaryEdges", "Meshes",
    Description = "Extracts the boundary edges of a mesh and draws them in red over the gray surface.",
    SourceFiles = new[] { "Examples/Meshes/BoundaryEdges/BoundaryEdges.cs" })]
internal sealed class BoundaryEdges : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/Meshes/BoundaryEdges/
        using var colors = vtkNamedColors.New();

        using var diskSource = vtkDiskSource.New();
        diskSource.Update();

        using var featureEdges = vtkFeatureEdges.New();
        featureEdges.SetInputConnection(diskSource.GetOutputPort());
        featureEdges.BoundaryEdgesOn();
        featureEdges.FeatureEdgesOff();
        featureEdges.ManifoldEdgesOff();
        featureEdges.NonManifoldEdgesOff();
        featureEdges.ColoringOn();
        featureEdges.Update();

        // Visualize
        using var edgeMapper = vtkPolyDataMapper.New();
        edgeMapper.SetInputConnection(featureEdges.GetOutputPort());
        edgeMapper.SetScalarModeToUseCellData();

        using var edgeActor = vtkActor.New();
        edgeActor.SetMapper(edgeMapper);

        using var diskMapper = vtkPolyDataMapper.New();
        diskMapper.SetInputConnection(diskSource.GetOutputPort());

        using var diskActor = vtkActor.New();
        diskActor.SetMapper(diskMapper);
        diskActor.GetProperty().SetColor(colors.GetColor3d("Gray"));

        // Create a renderer, render window, and interactor
        using var renderer = vtkRenderer.New();
        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);
        renderWindow.SetWindowName("BoundaryEdges");

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderer.AddActor(edgeActor);
        renderer.AddActor(diskActor);
        renderer.SetBackground(colors.GetColor3d("DimGray"));

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

        Debug.WriteLine("BoundaryEdges example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
