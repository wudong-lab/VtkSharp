using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ExtractSelection", "PolyData",
    Description = "Extracts point IDs 10 through 19 from a generated point cloud.",
    SourceFiles = new[] { "Examples/PolyData/ExtractSelection/ExtractSelection.cs" })]
internal sealed class ExtractSelection : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var points = vtkPointSource.New();
        points.SetNumberOfPoints(50);
        points.Update();
        using var selection = vtkSelectionSource.New();
        selection.SetFieldType(1); // POINT
        selection.SetContentType(3); // INDICES
        for (var id = 10; id < 20; id++) selection.AddID(0, 0, id);
        using var extract = vtkExtractSelection.New();
        extract.SetInputConnection(0, points.GetOutputPort());
        extract.SetInputConnection(1, selection.GetOutputPort());
        extract.Update();

        using var inputMapper = vtkDataSetMapper.New();
        inputMapper.SetInputConnection(points.GetOutputPort());
        using var inputActor = vtkActor.New();
        inputActor.SetMapper(inputMapper);
        inputActor.GetProperty().SetColor(colors.GetColor3d("LightGray"));
        inputActor.GetProperty().SetPointSize(5);
        using var selectedMapper = vtkDataSetMapper.New();
        selectedMapper.SetInputConnection(extract.GetOutputPort());
        using var selectedActor = vtkActor.New();
        selectedActor.SetMapper(selectedMapper);
        selectedActor.GetProperty().SetColor(colors.GetColor3d("Tomato"));
        selectedActor.GetProperty().SetPointSize(9);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(inputActor);
        renderer.AddActor(selectedActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("ExtractSelection");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        using var style = vtkInteractorStyleTrackballCamera.New();
        interactor.SetInteractorStyle(style);
        Debug.WriteLine($"Input contains 50 points; selected point IDs 10-19.");

        ExampleRenderSupport.Finish(window, interactor, "ExtractSelection", screenshotPath);
    }
}
