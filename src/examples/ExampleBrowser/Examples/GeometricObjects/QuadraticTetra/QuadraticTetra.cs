using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("QuadraticTetra", "GeometricObjects", Description = "Tessellates a quadratic tetrahedron and marks its ten nodes.", SourceFiles = new[] { "Examples/GeometricObjects/QuadraticTetra/QuadraticTetra.cs" })]
internal sealed class QuadraticTetra : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // VTK example source: https://examples.vtk.org/site/Cxx/GeometricObjects/QuadraticTetra/
        using var colors = vtkNamedColors.New();
        using var tetra = vtkQuadraticTetra.New();
        using var points = vtkPoints.New();
        points.SetNumberOfPoints(10);
        using var rng = vtkMinimalStandardRandomSequence.New();
        rng.SetSeed(5070);
        var parametric = new[] { 0d,0,0, 1,0,0, 0,1,0, 0,0,1, 0.5,0,0, 0.5,0.5,0, 0,0.5,0, 0,0,0.5, 0.5,0,0.5, 0,0.5,0.5 };
        for (var i = 0; i < 10; ++i)
        {
            var xyz = new double[3];
            for (var axis = 0; axis < 3; ++axis)
            {
                rng.Next();
                xyz[axis] = parametric[i * 3 + axis] + rng.GetRangeValue(-0.1, 0.1);
            }
            tetra.GetPointIds().SetId(i, i);
            points.SetPoint(i, xyz[0], xyz[1], xyz[2]);
        }
        using var grid = vtkUnstructuredGrid.New();
        grid.SetPoints(points);
        grid.InsertNextCell(24, tetra.GetPointIds()); // VTK_QUADRATIC_TETRA
        using var tessellate = vtkTessellatorFilter.New();
        tessellate.SetInputDataObject(0, grid);
        using var mapper = vtkDataSetMapper.New();
        mapper.SetInputConnection(tessellate.GetOutputPort());
        mapper.ScalarVisibilityOff();
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(colors.GetColor3d("Tomato"));
        actor.GetProperty().SetEdgeColor(colors.GetColor3d("IvoryBlack"));
        actor.GetProperty().EdgeVisibilityOn();
        using var sphere = vtkSphereSource.New();
        sphere.SetRadius(0.02);
        using var glyphs = vtkGlyph3D.New();
        glyphs.SetInputData(grid);
        glyphs.SetSourceConnection(sphere.GetOutputPort());
        glyphs.ScalingOff();
        using var glyphMapper = vtkDataSetMapper.New();
        glyphMapper.SetInputConnection(glyphs.GetOutputPort());
        glyphMapper.ScalarVisibilityOff();
        using var glyphActor = vtkActor.New();
        glyphActor.SetMapper(glyphMapper);
        glyphActor.GetProperty().SetColor(colors.GetColor3d("Banana"));
        using var renderer = vtkRenderer.New();
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("QuadraticTetra");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        renderer.AddActor(actor);
        renderer.AddActor(glyphActor);
        renderer.SetBackground(colors.GetColor3d("SlateGray"));
        renderer.ResetCamera();
        ExampleRenderSupport.Finish(window, interactor, "QuadraticTetra", screenshotPath);
    }
}
