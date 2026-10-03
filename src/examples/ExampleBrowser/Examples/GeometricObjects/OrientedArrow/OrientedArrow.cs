using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("OrientedArrow", "GeometricObjects",
    Description = "Places an arrow between two points using a right-handed local basis.",
    SourceFiles = new[] { "Examples/GeometricObjects/OrientedArrow/OrientedArrow.cs" })]
internal class OrientedArrow : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example aligns an arrow's local X axis to a start/end vector:
        // https://examples.vtk.org/site/Cxx/GeometricObjects/OrientedArrow/
        double[] start = { -2, -1, 0 };
        double[] end = { 3, 4, 2 };
        var x = Normalize(Subtract(end, start));
        var reference = Math.Abs(x[2]) > 0.95 ? new[] { 1d, 0, 0 } : new[] { 0d, 0, 1 };
        var z = Normalize(Cross(x, reference));
        var y = Cross(z, x);
        var length = Distance(start, end);

        using var matrix = vtkMatrix4x4.New();
        matrix.Identity();
        for (int row = 0; row < 3; row++)
        {
            matrix.SetElement(row, 0, x[row] * length);
            matrix.SetElement(row, 1, y[row] * length);
            matrix.SetElement(row, 2, z[row] * length);
            matrix.SetElement(row, 3, start[row]);
        }

        using var colors = vtkNamedColors.New();
        using var arrowSource = vtkArrowSource.New();
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(arrowSource.GetOutputPort());
        using var arrow = vtkActor.New();
        arrow.SetMapper(mapper);
        arrow.SetUserMatrix(matrix);
        var cyan = colors.GetColor3d("Cyan");
        arrow.GetProperty().SetColor(cyan.R, cyan.G, cyan.B);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(arrow);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("OrientedArrow");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "OrientedArrow", screenshotPath);
    }

    private static double[] Subtract(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    private static double[] Cross(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    private static double[] Normalize(double[] a) { var m = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); return new[] { a[0] / m, a[1] / m, a[2] / m }; }
    private static double Distance(double[] a, double[] b) { var d = Subtract(a, b); return Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]); }
}
