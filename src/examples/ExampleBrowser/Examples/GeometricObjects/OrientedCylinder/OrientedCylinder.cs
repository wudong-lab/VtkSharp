using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("OrientedCylinder", "GeometricObjects",
    Description = "Places a cylinder between two points with a local Y axis along the member.",
    SourceFiles = new[] { "Examples/GeometricObjects/OrientedCylinder/OrientedCylinder.cs" })]
internal class OrientedCylinder : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example aligns a cylinder's local Y axis to a start/end vector:
        // https://examples.vtk.org/site/Cxx/GeometricObjects/OrientedCylinder/
        double[] start = { -2, -1, 0 };
        double[] end = { 3, 4, 2 };
        var axis = Normalize(Subtract(end, start));
        var reference = Math.Abs(axis[2]) > 0.95 ? new[] { 1d, 0, 0 } : new[] { 0d, 0, 1 };
        var localX = Normalize(Cross(reference, axis));
        var localZ = Cross(localX, axis);
        var length = Distance(start, end);

        using var matrix = vtkMatrix4x4.New();
        matrix.Identity();
        var middle = new[] { (start[0] + end[0]) / 2, (start[1] + end[1]) / 2, (start[2] + end[2]) / 2 };
        for (int row = 0; row < 3; row++)
        {
            matrix.SetElement(row, 0, localX[row]);
            matrix.SetElement(row, 1, axis[row] * length);
            matrix.SetElement(row, 2, localZ[row]);
            matrix.SetElement(row, 3, middle[row]);
        }

        using var colors = vtkNamedColors.New();
        using var cylinderSource = vtkCylinderSource.New();
        cylinderSource.SetResolution(24);
        cylinderSource.SetRadius(0.2);
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cylinderSource.GetOutputPort());
        using var cylinder = vtkActor.New();
        cylinder.SetMapper(mapper);
        cylinder.SetUserMatrix(matrix);
        var cyan = colors.GetColor3d("Cyan");
        cylinder.GetProperty().SetColor(cyan.R, cyan.G, cyan.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(cylinder);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("OrientedCylinder");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "OrientedCylinder", screenshotPath);
    }

    private static double[] Subtract(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    private static double[] Cross(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    private static double[] Normalize(double[] a) { var m = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); return new[] { a[0] / m, a[1] / m, a[2] / m }; }
    private static double Distance(double[] a, double[] b) { var d = Subtract(a, b); return Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]); }
}
