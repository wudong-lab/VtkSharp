using System.IO;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("DEMReader", "IO",
    Description = "Reads a USGS DEM elevation grid and displays it with a continuous elevation color map.",
    SourceFiles = new[] { "Examples/IO/DEMReader/DEMReader.cs" })]
internal sealed class DEMReader : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/IO/DEMReader/
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "SainteHelens.dem");
        if (!File.Exists(dataPath))
            throw new FileNotFoundException("The SainteHelens DEM example data was not found.", dataPath);

        using var colors = vtkNamedColors.New();
        using var reader = vtkDEMReader.New();
        reader.SetFileName(dataPath);
        reader.Update();

        using var dem = reader.GetOutput();
        Span<double> scalarRange = stackalloc double[2];
        dem.GetScalarRange(scalarRange);

        using var lookupTable = vtkLookupTable.New();
        lookupTable.SetHueRange(0.6, 0.0);
        lookupTable.SetSaturationRange(1.0, 0.0);
        lookupTable.SetValueRange(0.5, 1.0);
        lookupTable.SetTableRange(scalarRange);

        using var colorMap = vtkImageMapToColors.New();
        colorMap.SetLookupTable(lookupTable);
        colorMap.SetInputConnection(reader.GetOutputPort());

        using var actor = vtkImageActor.New();
        using var imageMapper = actor.GetMapper();
        imageMapper.SetInputConnection(colorMap.GetOutputPort());

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.ResetCamera();

        using var window = vtkRenderWindow.New();
        window.SetWindowName("DEMReader");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        using var style = vtkInteractorStyleImage.New();
        interactor.SetInteractorStyle(style);
        interactor.SetRenderWindow(window);

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
