using System.Diagnostics;
using System.IO;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("PointInterpolator", "Meshes",
    Description = "Interpolates sparse scalar samples onto an STL surface with a Gaussian kernel.",
    SourceFiles = new[] { "Examples/Meshes/PointInterpolator/PointInterpolator.cs" })]
internal sealed class PointInterpolator : ISmokeExample
{
    public void Run() => Render(null);

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/Meshes/PointInterpolator/
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var pointsPath = Path.Combine(dataDirectory, "sparsePoints.txt");
        var surfacePath = Path.Combine(dataDirectory, "InterpolatingOnSTL_final.stl");
        EnsureDataFile(pointsPath);
        EnsureDataFile(surfacePath);

        using var pointsReader = vtkDelimitedTextReader.New();
        pointsReader.SetFileName(pointsPath);
        pointsReader.DetectNumericColumnsOn();
        pointsReader.SetFieldDelimiterCharacters("\t");
        pointsReader.SetHaveHeaders(true);

        using var tablePoints = vtkTableToPolyData.New();
        tablePoints.SetInputConnection(pointsReader.GetOutputPort());
        tablePoints.SetXColumn("x");
        tablePoints.SetYColumn("y");
        tablePoints.SetZColumn("z");
        tablePoints.Update();

        var points = tablePoints.GetOutput();
        var pointData = points.GetPointData();
        pointData.SetActiveScalars("val");
        var scalars = pointData.GetScalars();
        Span<double> scalarRange = stackalloc double[2];
        scalars.GetRange(scalarRange);

        using var stlReader = vtkSTLReader.New();
        stlReader.SetFileName(surfacePath);
        stlReader.Update();
        var surface = stlReader.GetOutput();

        using var gaussianKernel = vtkGaussianKernel.New();
        gaussianKernel.SetSharpness(2.0);
        gaussianKernel.SetRadius(12.0);

        using var interpolator = vtkPointInterpolator.New();
        interpolator.SetInputData(surface);
        interpolator.SetSourceData(points);
        interpolator.SetKernel(gaussianKernel);

        using var surfaceMapper = vtkPolyDataMapper.New();
        surfaceMapper.SetInputConnection(interpolator.GetOutputPort());
        surfaceMapper.SetScalarRange(scalarRange);
        using var surfaceActor = vtkActor.New();
        surfaceActor.SetMapper(surfaceMapper);

        using var pointsMapper = vtkPointGaussianMapper.New();
        pointsMapper.SetInputData(points);
        pointsMapper.SetScalarRange(scalarRange);
        pointsMapper.SetScaleFactor(0.6);
        pointsMapper.EmissiveOff();
        pointsMapper.SetSplatShaderCode("""
            //VTK::Color::Impl
            float dist = dot(offsetVCVSOutput.xy, offsetVCVSOutput.xy);
            if (dist > 1.0) {
              discard;
            } else {
              float scale = (1.0 - dist);
              ambientColor *= scale;
              diffuseColor *= scale;
            };
            """);
        using var pointsActor = vtkActor.New();
        pointsActor.SetMapper(pointsMapper);

        using var colors = vtkNamedColors.New();
        using var renderer = vtkRenderer.New();
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.AddActor(surfaceActor);
        renderer.AddActor(pointsActor);

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);
        renderWindow.SetSize(640, 480);
        renderWindow.SetWindowName("PointInterpolator");

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(renderWindow);

        renderWindow.Render();
        renderer.ResetCamera();
        renderer.GetActiveCamera().Elevation(-45.0);
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

        Debug.WriteLine("PointInterpolator example running. Close the window to exit.");
        interactor.Start();
    }

    private static void EnsureDataFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("PointInterpolator example data was not found.", path);
    }
}
