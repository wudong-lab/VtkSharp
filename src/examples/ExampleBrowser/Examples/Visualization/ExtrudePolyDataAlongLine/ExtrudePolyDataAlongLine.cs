using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ExtrudePolyDataAlongLine", "Visualization",
    Description = "Sweeps a closed circular section along a 3D curve and closes both ends.",
    SourceFiles = new[] { "Examples/Visualization/ExtrudePolyDataAlongLine/ExtrudePolyDataAlongLine.cs" })]
internal class ExtrudePolyDataAlongLine : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example generates a section when no file is supplied, then sweeps it along a spline:
        // https://examples.vtk.org/site/Cxx/Visualization/ExtrudePolyDataAlongLine/
        const int stationCount = 81;
        const int sideCount = 16;
        const double radius = 0.16;
        using var points = vtkPoints.New();
        using var polygons = vtkCellArray.New();
        var initialTangent = Tangent(0);
        var reference = Math.Abs(initialTangent[2]) > 0.95 ? new[] { 1d, 0, 0 } : new[] { 0d, 0, 1 };
        var normal = Normalize(Cross(reference, initialTangent));

        for (int station = 0; station < stationCount; station++)
        {
            var t = (double)station / (stationCount - 1);
            var center = Curve(t);
            var tangent = Tangent(t);
            // Project the previous section axis onto the new normal plane to avoid Frenet-frame flips.
            normal = Normalize(Subtract(normal, Scale(tangent, Dot(normal, tangent))));
            var binormal = Normalize(Cross(tangent, normal));
            normal = Cross(binormal, tangent);

            for (int side = 0; side < sideCount; side++)
            {
                var angle = 2 * Math.PI * side / sideCount;
                var offset = Add(Scale(normal, radius * Math.Cos(angle)), Scale(binormal, radius * Math.Sin(angle)));
                var p = Add(center, offset);
                points.InsertNextPoint(p[0], p[1], p[2]);
            }
        }

        for (int station = 0; station < stationCount - 1; station++)
        {
            for (int side = 0; side < sideCount; side++)
            {
                var nextSide = (side + 1) % sideCount;
                polygons.InsertNextCell(4);
                polygons.InsertCellPoint(station * sideCount + side);
                polygons.InsertCellPoint(station * sideCount + nextSide);
                polygons.InsertCellPoint((station + 1) * sideCount + nextSide);
                polygons.InsertCellPoint((station + 1) * sideCount + side);
            }
        }

        polygons.InsertNextCell(sideCount);
        for (int side = sideCount - 1; side >= 0; side--)
        {
            polygons.InsertCellPoint(side);
        }
        var endOffset = (stationCount - 1) * sideCount;
        polygons.InsertNextCell(sideCount);
        for (int side = 0; side < sideCount; side++)
        {
            polygons.InsertCellPoint(endOffset + side);
        }

        using var sweptSurface = vtkPolyData.New();
        sweptSurface.SetPoints(points);
        sweptSurface.SetPolys(polygons);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputData(sweptSurface);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var colors = vtkNamedColors.New();
        var faceColor = colors.GetColor3d("BurlyWood");
        actor.GetProperty().SetColor(faceColor.R, faceColor.G, faceColor.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.ResetCamera();
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(640, 480);
        window.SetWindowName("ExtrudePolyDataAlongLine");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "ExtrudePolyDataAlongLine", screenshotPath);
    }

    private static double[] Curve(double t) => new[] { 8 * t, 1.8 * Math.Sin(2 * Math.PI * t), 0.8 * Math.Sin(4 * Math.PI * t) };
    private static double[] Tangent(double t) => Normalize(new[] { 8d, 3.6 * Math.PI * Math.Cos(2 * Math.PI * t), 3.2 * Math.PI * Math.Cos(4 * Math.PI * t) });
    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    private static double[] Add(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };
    private static double[] Subtract(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    private static double[] Scale(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };
    private static double[] Cross(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    private static double[] Normalize(double[] a) { var m = Math.Sqrt(Dot(a, a)); return Scale(a, 1 / m); }
}
