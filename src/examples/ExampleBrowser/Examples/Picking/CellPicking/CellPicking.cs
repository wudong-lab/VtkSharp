using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("CellPicking", "Picking", Description = "Picks triangle cells on click and reports the selected cell ID and world position.", SourceFiles = new[] { "Examples/Picking/CellPicking/CellPicking.cs" })]
internal sealed class CellPicking : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New(); using var plane = vtkPlaneSource.New();
        using var triangles = vtkTriangleFilter.New(); triangles.SetInputConnection(plane.GetOutputPort());
        using var mapper = vtkPolyDataMapper.New(); mapper.SetInputConnection(triangles.GetOutputPort());
        using var actor = vtkActor.New(); actor.SetMapper(mapper); actor.GetProperty().SetColor(colors.GetColor3d("SeaGreen"));
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("CellPicking");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        using var picker = vtkCellPicker.New(); picker.SetTolerance(0.0005);
        using var selection = vtkSelectionSource.New(); selection.SetFieldType(0); selection.SetContentType(3); // CELL, INDICES
        using var extract = vtkExtractSelection.New(); extract.SetInputConnection(0, triangles.GetOutputPort()); extract.SetInputConnection(1, selection.GetOutputPort());
        using var selectedMapper = vtkDataSetMapper.New(); selectedMapper.SetInputConnection(extract.GetOutputPort());
        using var selectedActor = vtkActor.New(); selectedActor.SetMapper(selectedMapper);
        selectedActor.GetProperty().EdgeVisibilityOn(); selectedActor.GetProperty().SetLineWidth(3); selectedActor.GetProperty().SetColor(colors.GetColor3d("Tomato"));
        using var observer = interactor.AddObserver(vtkCommand.LeftButtonPressEvent, OnLeftButtonDown,
            clientData: (picker, renderer, selection, extract, selectedActor, window));
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("PaleTurquoise")); renderer.ResetCamera();
        renderer.AddActor(selectedActor);
        ExampleRenderSupport.Finish(window, interactor, "CellPicking", screenshotPath);
    }

    private static void OnLeftButtonDown(vtkObject caller, uint eventId, object? clientData, nint callData)
    {
        var interactor = (vtkRenderWindowInteractor)caller;
        var (picker, renderer, selection, extract, selectedActor, window) =
            ((vtkCellPicker Picker, vtkRenderer Renderer, vtkSelectionSource Selection,
                vtkExtractSelection Extract, vtkActor SelectedActor, vtkRenderWindow Window))clientData!;
        interactor.GetEventPosition(out var x, out var y);
        picker.Pick(x, y, 0, renderer);
        if (picker.GetCellId() >= 0)
        {
            selection.AddID(0, 0, picker.GetCellId());
            extract.Update();
            window.Render();
            Debug.WriteLine($"Highlighted cell {picker.GetCellId()}");
        }
    }
}
