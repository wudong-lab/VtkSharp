using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("WarpVector", "PolyData",
    Description = "Builds sampled beam curves from nodal translations and rotations, then warps the frame.",
    SourceFiles = new[] { "Examples/PolyData/WarpVector/WarpVector.cs" })]
internal class WarpVector : ISmokeExample
{
    private const int SamplesPerElement = 16;

    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example moves line points with a point-vector array:
        // https://examples.vtk.org/site/Cxx/PolyData/WarpVector/
        // Hermite interpolation adds beam rotation DOFs so a two-node member can bend between its ends.
        double[][] positions =
        {
            new[] { 0d, 0, 0 },
            new[] { 4d, 0, 0 },
            new[] { 8d, 1, 0 }
        };
        double[][] translations =
        {
            new[] { 0d, 0, 0 },
            new[] { 0d, 0.1, 0 },
            new[] { 0d, 0.35, 0.1 }
        };
        // Nodal rotations are global radians about X/Y/Z; local end slopes follow a right-handed frame.
        double[][] rotations =
        {
            new[] { 0d, 0, 0 },
            new[] { 0d, 0, 0.15 },
            new[] { 0d, 0.05, 0.05 }
        };
        (int Start, int End)[] elements = { (0, 1), (1, 2) };

        using var points = vtkPoints.New();
        using var lines = vtkCellArray.New();
        using var displacement = vtkDoubleArray.New();
        displacement.SetName("Displacement");
        displacement.SetNumberOfComponents(3);
        foreach (var (startIndex, endIndex) in elements)
        {
            var start = positions[startIndex];
            var end = positions[endIndex];
            var delta = Subtract(end, start);
            var length = Magnitude(delta);
            var tangent = Scale(delta, 1 / length);
            var reference = Math.Abs(tangent[2]) > 0.95 ? new[] { 1d, 0, 0 } : new[] { 0d, 0, 1 };
            var localY = Normalize(Cross(reference, tangent));
            var localZ = Cross(tangent, localY);

            var u0 = translations[startIndex];
            var u1 = translations[endIndex];
            var r0 = rotations[startIndex];
            var r1 = rotations[endIndex];
            var axial0 = Dot(u0, tangent);
            var axial1 = Dot(u1, tangent);
            var transverseY0 = Dot(u0, localY);
            var transverseY1 = Dot(u1, localY);
            var transverseZ0 = Dot(u0, localZ);
            var transverseZ1 = Dot(u1, localZ);
            var rotationY0 = Dot(r0, localY);
            var rotationY1 = Dot(r1, localY);
            var rotationZ0 = Dot(r0, localZ);
            var rotationZ1 = Dot(r1, localZ);

            lines.InsertNextCell(SamplesPerElement + 1);
            for (int sample = 0; sample <= SamplesPerElement; sample++)
            {
                var t = (double)sample / SamplesPerElement;
                var t2 = t * t;
                var t3 = t2 * t;
                var h1 = 1 - 3 * t2 + 2 * t3;
                var h2 = t - 2 * t2 + t3;
                var h3 = 3 * t2 - 2 * t3;
                var h4 = -t2 + t3;
                var axial = (1 - t) * axial0 + t * axial1;
                var deflectionY = h1 * transverseY0 + h2 * length * rotationZ0 + h3 * transverseY1 + h4 * length * rotationZ1;
                var deflectionZ = h1 * transverseZ0 - h2 * length * rotationY0 + h3 * transverseZ1 - h4 * length * rotationY1;
                var undeformed = Add(start, Scale(delta, t));
                var vector = Add(Scale(tangent, axial), Add(Scale(localY, deflectionY), Scale(localZ, deflectionZ)));
                points.InsertNextPoint(undeformed[0], undeformed[1], undeformed[2]);
                displacement.InsertNextTuple3(vector[0], vector[1], vector[2]);
                lines.InsertCellPoint((points.GetNumberOfPoints() - 1));
            }
        }

        using var frame = vtkPolyData.New();
        frame.SetPoints(points);
        frame.SetLines(lines);
        frame.GetPointData().AddArray(displacement);
        frame.GetPointData().SetActiveVectors("Displacement");

        using var originalMapper = vtkPolyDataMapper.New();
        originalMapper.SetInputData(frame);
        using var originalActor = vtkActor.New();
        originalActor.SetMapper(originalMapper);
        using var warped = vtkWarpVector.New();
        warped.SetInputData(frame);
        warped.SetScaleFactor(4);
        warped.Update();
        using var warpedMapper = vtkPolyDataMapper.New();
        warpedMapper.SetInputData(warped.GetPolyDataOutput());
        using var warpedActor = vtkActor.New();
        warpedActor.SetMapper(warpedMapper);
        using var colors = vtkNamedColors.New();
        var originalColor = colors.GetColor3d("Gray");
        originalActor.GetProperty().SetColor(originalColor.R, originalColor.G, originalColor.B);
        originalActor.GetProperty().SetLineWidth(2);
        var warpedColor = colors.GetColor3d("Tomato");
        warpedActor.GetProperty().SetColor(warpedColor.R, warpedColor.G, warpedColor.B);
        warpedActor.GetProperty().SetLineWidth(4);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(originalActor);
        renderer.AddActor(warpedActor);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("WarpVector - beam displacement");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "WarpVector", screenshotPath);
    }

    private static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    private static double[] Add(double[] a, double[] b) => new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] };
    private static double[] Subtract(double[] a, double[] b) => new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    private static double[] Scale(double[] a, double s) => new[] { a[0] * s, a[1] * s, a[2] * s };
    private static double[] Cross(double[] a, double[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
    private static double Magnitude(double[] a) => Math.Sqrt(Dot(a, a));
    private static double[] Normalize(double[] a) => Scale(a, 1 / Magnitude(a));
}
