using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("LinearExtrusion", "PolyData",
    Description = "Extrudes an I-shaped beam section along an element axis using its local frame.",
    SourceFiles = new[] { "Examples/PolyData/LinearExtrusion/LinearExtrusion.cs" })]
internal class LinearExtrusion : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example extrudes vector text; this version extrudes a beam section:
        // https://examples.vtk.org/site/Cxx/PolyData/LinearExtrusion/
        double[] start = { 0, 0, 0 };
        double[] end = { 4, 2, 1 };
        var delta = Subtract(end, start);
        var length = Magnitude(delta);
        var axis = Scale(delta, 1 / length);
        var reference = Math.Abs(axis[2]) > 0.95 ? new[] { 1d, 0, 0 } : new[] { 0d, 0, 1 };
        var localY = Normalize(Cross(reference, axis));
        var localZ = Cross(axis, localY);

        // I-section outline in the element-local Y/Z plane.
        double[,] outline =
        {
            { -0.4, 0.4 }, { 0.4, 0.4 }, { 0.4, 0.26 }, { 0.06, 0.26 },
            { 0.06, -0.26 }, { 0.4, -0.26 }, { 0.4, -0.4 }, { -0.4, -0.4 },
            { -0.4, -0.26 }, { -0.06, -0.26 }, { -0.06, 0.26 }, { -0.4, 0.26 }
        };
        using var points = vtkPoints.New();
        using var contour = vtkCellArray.New();
        contour.InsertNextCell(outline.GetLength(0));
        for (int i = 0; i < outline.GetLength(0); i++)
        {
            var point = Add(start, Add(Scale(localY, outline[i, 0]), Scale(localZ, outline[i, 1])));
            points.InsertNextPoint(point[0], point[1], point[2]);
            contour.InsertCellPoint(i);
        }

        using var section = vtkPolyData.New();
        section.SetPoints(points);
        section.SetPolys(contour);
        using var extrusion = vtkLinearExtrusionFilter.New();
        extrusion.SetInputData(section);
        extrusion.SetExtrusionTypeToVectorExtrusion();
        extrusion.SetVector(delta[0], delta[1], delta[2]);
        extrusion.SetScaleFactor(1);
        extrusion.CappingOn();

        using var triangles = vtkTriangleFilter.New();
        triangles.SetInputConnection(extrusion.GetOutputPort());
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(triangles.GetOutputPort());
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        using var colors = vtkNamedColors.New();
        var beamColor = colors.GetColor3d("PaleGoldenrod");
        actor.GetProperty().SetColor(beamColor.R, beamColor.G, beamColor.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.ResetCamera();
        renderer.GetActiveCamera().Azimuth(35);
        renderer.GetActiveCamera().Elevation(25);
        renderer.ResetCameraClippingRange();
        var background = colors.GetColor3d("SteelBlue");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("LinearExtrusion - I section");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "LinearExtrusion", screenshotPath);
    }

    private static double[] Add(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };
    private static double[] Subtract(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    private static double[] Scale(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };
    private static double[] Cross(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    private static double Magnitude(double[] a) => Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
    private static double[] Normalize(double[] a) => Scale(a, 1 / Magnitude(a));
}
