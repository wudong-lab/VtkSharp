using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("GreedyTerrainDecimation", "PolyData",
    Description = "Converts a small elevation grid to a terrain mesh and displays its triangulation.",
    SourceFiles = new[] { "Examples/PolyData/GreedyTerrainDecimation/GreedyTerrainDecimation.cs" })]
internal sealed class GreedyTerrainDecimation : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // https://examples.vtk.org/site/Cxx/PolyData/GreedyTerrainDecimation/
        using var image = vtkImageData.New();
        image.SetDimensions(3, 3, 1);
        image.AllocateScalars(3, 1); // VTK_UNSIGNED_CHAR

        using var random = vtkMinimalStandardRandomSequence.New();
        random.SetSeed(8775070);
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var elevation = Math.Floor(random.GetRangeValue(0, 5) + 0.5);
                image.SetScalarComponentFromDouble(x, y, 0, 0, elevation);
                random.Next();
            }
        }

        using var decimation = vtkGreedyTerrainDecimation.New();
        decimation.SetInputData(image);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(decimation.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().EdgeVisibilityOn();
        actor.GetProperty().SetEdgeColor(1, 0, 0);
        actor.GetProperty().SetInterpolationToFlat();

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.AddActor(actor);

        using var window = vtkRenderWindow.New();
        window.SetWindowName("GreedyTerrainDecimation");
        window.SetSize(640, 480);
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        renderer.ResetCamera();
        window.Render();
        if (screenshotPath is not null)
        {
            using var result = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(result);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        interactor.Start();
    }
}
