using System.Diagnostics;
using System.IO;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ContourTriangulator", "Modelling",
    Description = "Extracts iso-contours from a PNG image with marching squares, then fills them into triangulated polygons.",
    SourceFiles = new[] { "Examples/Modelling/ContourTriangulator/ContourTriangulator.cs" })]
internal sealed class ContourTriangulator : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // 从 PNG 图像提取等值线，并用 vtkContourTriangulator 将等值线填充为三角化曲面。
        // https://examples.vtk.org/site/Cxx/Modelling/ContourTriangulator/
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var inputFileName = Path.Combine(dataDirectory, "fullhead15.png");
        if (!File.Exists(inputFileName))
            throw new FileNotFoundException("ContourTriangulator example data was not found.", inputFileName);
        const int isoValue = 500;

        using var colors = vtkNamedColors.New();

        using var reader = vtkPNGReader.New();
        if (reader.CanReadFile(inputFileName) == 0)
        {
            Debug.WriteLine($"Error: Could not read {inputFileName}.");
            return;
        }
        reader.SetFileName(inputFileName);
        reader.Update();

        using var iso = vtkMarchingSquares.New();
        iso.SetInputConnection(reader.GetOutputPort());
        iso.SetValue(0, isoValue);

        using var isoMapper = vtkDataSetMapper.New();
        isoMapper.SetInputConnection(iso.GetOutputPort());
        isoMapper.ScalarVisibilityOff();

        using var isoActor = vtkActor.New();
        isoActor.SetMapper(isoMapper);
        var mediumOrchid = colors.GetColor3d("MediumOrchid");
        isoActor.GetProperty().SetColor(mediumOrchid.R, mediumOrchid.G, mediumOrchid.B);

        using var poly = vtkContourTriangulator.New();
        poly.SetInputConnection(iso.GetOutputPort());

        using var polyMapper = vtkDataSetMapper.New();
        polyMapper.SetInputConnection(poly.GetOutputPort());
        polyMapper.ScalarVisibilityOff();

        using var polyActor = vtkActor.New();
        polyActor.SetMapper(polyMapper);
        var gray = colors.GetColor3d("Gray");
        polyActor.GetProperty().SetColor(gray.R, gray.G, gray.B);

        // Standard rendering classes
        using var renderer = vtkRenderer.New();
        using var renWin = vtkRenderWindow.New();
        renWin.SetMultiSamples(0);
        renWin.AddRenderer(renderer);
        renWin.SetWindowName("ContourTriangulator");

        using var iren = vtkRenderWindowInteractor.New();
        iren.SetRenderWindow(renWin);

        renderer.AddActor(polyActor);
        renderer.AddActor(isoActor);
        var darkSlateGray = colors.GetColor3d("DarkSlateGray");
        renderer.SetBackground(darkSlateGray.R, darkSlateGray.G, darkSlateGray.B);
        renWin.SetSize(300, 300);

        var camera = renderer.GetActiveCamera();
        renderer.ResetCamera();
        camera.Azimuth(180);

        renWin.Render();

        if (screenshotPath is not null)
        {
            using var image = renWin.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        iren.Initialize();
        iren.Start();
    }
}
