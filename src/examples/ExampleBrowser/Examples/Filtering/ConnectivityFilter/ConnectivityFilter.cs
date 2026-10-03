using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ConnectivityFilter", "Filtering", Description = "Labels connected regions on a polydata surface.", SourceFiles = new[] { "Examples/Filtering/ConnectivityFilter/ConnectivityFilter.cs" })]
internal sealed class ConnectivityFilter : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var colors = vtkNamedColors.New();
        using var source = vtkSphereSource.New();
        using var connected = vtkPolyDataConnectivityFilter.New(); connected.SetInputConnection(source.GetOutputPort()); connected.SetExtractionModeToAllRegions(); connected.ColorRegionsOn();
        using var mapper = vtkPolyDataMapper.New(); mapper.SetInputConnection(connected.GetOutputPort()); mapper.SetScalarModeToUseCellData();
        using var actor = vtkActor.New(); actor.SetMapper(mapper);
        using var renderer = vtkRenderer.New(); using var window = vtkRenderWindow.New(); window.AddRenderer(renderer); window.SetWindowName("ConnectivityFilter");
        using var interactor = vtkRenderWindowInteractor.New(); interactor.SetRenderWindow(window);
        renderer.AddActor(actor); renderer.SetBackground(colors.GetColor3d("SlateGray"));
        ExampleRenderSupport.Finish(window, interactor, "ConnectivityFilter", screenshotPath);
    }
}
