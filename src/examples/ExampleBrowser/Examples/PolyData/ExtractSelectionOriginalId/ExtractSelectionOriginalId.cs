using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ExtractSelectionOriginalId", "PolyData", Description = "Extracts point IDs 10 through 19 and displays the extracted subset beside its input.", SourceFiles = new[] { "Examples/PolyData/ExtractSelectionOriginalId/ExtractSelectionOriginalId.cs" })]
internal sealed class ExtractSelectionOriginalId : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var source = vtkPointSource.New(); source.SetNumberOfPoints(50); source.Update();
        using var selectionSource = vtkSelectionSource.New(); selectionSource.SetFieldType(1); selectionSource.SetContentType(3); // POINT, INDICES
        for (var id = 10; id < 20; ++id) selectionSource.AddID(0, 0, id);
        using var extract = vtkExtractSelection.New(); extract.SetInputConnection(0, source.GetOutputPort()); extract.SetInputConnection(1, selectionSource.GetOutputPort());
        extract.Update();
        using var outputObject = extract.GetOutputDataObject(0);
        using var selected = vtkUnstructuredGrid.FromBorrowedPointer(outputObject.NativePointer);
        using var originalIds = selected.GetPointData().GetArray("vtkOriginalPointIds");
        for (var extractedId = 0; extractedId < selected.GetNumberOfPoints(); ++extractedId)
            Debug.WriteLine($"Extracted point {extractedId} was originally point {(long)originalIds.GetTuple1(extractedId)}");
        using var inputMapper = vtkDataSetMapper.New(); inputMapper.SetInputConnection(source.GetOutputPort());
        using var inputActor = vtkActor.New(); inputActor.SetMapper(inputMapper); inputActor.GetProperty().SetColor(colors.GetColor3d("MidnightBlue")); inputActor.GetProperty().SetPointSize(5);
        using var selectedMapper = vtkDataSetMapper.New(); selectedMapper.SetInputConnection(extract.GetOutputPort());
        using var selectedActor = vtkActor.New(); selectedActor.SetMapper(selectedMapper); selectedActor.GetProperty().SetColor(colors.GetColor3d("Tomato")); selectedActor.GetProperty().SetPointSize(8);
        using var left = vtkRenderer.New(); left.SetViewport(0, 0, 0.5, 1); left.SetBackground(colors.GetColor3d("BurlyWood"));
        using var right = vtkRenderer.New(); right.SetViewport(0.5, 0, 1, 1); right.SetBackground(colors.GetColor3d("CornflowerBlue"));
        using var camera = vtkCamera.New(); left.SetActiveCamera(camera); right.SetActiveCamera(camera); left.AddActor(inputActor); right.AddActor(selectedActor); left.ResetCamera();
        using var window = vtkRenderWindow.New(); window.AddRenderer(left); window.AddRenderer(right); window.SetSize(600, 300); window.SetWindowName("ExtractSelectionOriginalId");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        ExampleRenderSupport.Finish(window, interactor, "ExtractSelectionOriginalId", screenshotPath);
    }
}
