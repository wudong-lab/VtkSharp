using System.IO;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ReadUnstructuredGrid", "IO", Description = "Reads and displays a VTU unstructured grid with visible cell edges.", SourceFiles = new[] { "Examples/IO/ReadUnstructuredGrid/ReadUnstructuredGrid.cs" })]
internal sealed class ReadUnstructuredGrid : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/IO/ReadUnstructuredGrid/
        using var reader = vtkXMLUnstructuredGridReader.New();
        reader.SetFileName(Path.Combine(AppContext.BaseDirectory, "Data", "hexahedron.vtu"));
        using var colors = vtkNamedColors.New();
        using var mapper = vtkDataSetMapper.New();
        mapper.SetInputConnection(reader.GetOutputPort());
        mapper.ScalarVisibilityOff();
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().EdgeVisibilityOn();
        actor.GetProperty().SetLineWidth(2);
        actor.GetProperty().SetColor(colors.GetColor3d("MistyRose"));
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(640, 480);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(actor);
        renderer.SetBackground(colors.GetColor3d("Wheat"));
        renderer.ResetCamera();
        ExampleRenderSupport.Finish(window, interactor, "ReadUnstructuredGrid", screenshotPath);
    }
}
