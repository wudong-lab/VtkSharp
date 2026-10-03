using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Glyph3DMapper", "Visualization",
    Description = "Shows nodes as batched glyphs with per-node color and three-component scale.",
    SourceFiles = new[] { "Examples/Visualization/Glyph3DMapper/Glyph3DMapper.cs" })]
internal class Glyph3DMapper : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example maps per-point colors and scale vectors to GPU glyphs:
        // https://examples.vtk.org/site/Cxx/Visualization/Glyph3DMapper/
        using var colors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0);
        points.InsertNextPoint(1, 1, 1);
        points.InsertNextPoint(2, 2, 2);

        using var scaleFactors = vtkFloatArray.New();
        scaleFactors.SetNumberOfComponents(3);
        scaleFactors.SetName("Scale Factors");
        scaleFactors.InsertNextTuple3(0.7, 1, 1);
        scaleFactors.InsertNextTuple3(1, 0.7, 1);
        scaleFactors.InsertNextTuple3(1, 1, 0.7);

        using var glyphColors = vtkUnsignedCharArray.New();
        glyphColors.SetName("Colors");
        glyphColors.SetNumberOfComponents(3);
        glyphColors.SetNumberOfTuples(3);
        var red = colors.GetColor3ub("Red");
        var green = colors.GetColor3ub("Green");
        var blue = colors.GetColor3ub("Blue");
        glyphColors.SetTuple3(0, red.R, red.G, red.B);
        glyphColors.SetTuple3(1, green.R, green.G, green.B);
        glyphColors.SetTuple3(2, blue.R, blue.G, blue.B);

        using var nodes = vtkPolyData.New();
        nodes.SetPoints(points);
        nodes.GetPointData().AddArray(glyphColors);
        nodes.GetPointData().AddArray(scaleFactors);

        using var cube = vtkCubeSource.New();
        using var glyphMapper = vtkGlyph3DMapper.New();
        glyphMapper.SetSourceConnection(cube.GetOutputPort());
        glyphMapper.SetInputData(nodes);
        glyphMapper.SetScalarModeToUsePointFieldData();
        glyphMapper.SetScaleArray("Scale Factors");
        glyphMapper.SetScaleModeToScaleByVectorComponents();
        glyphMapper.SelectColorArray("Colors");

        using var actor = vtkActor.New();
        actor.SetMapper(glyphMapper);
        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        renderer.GetActiveCamera().SetPosition(-10, 5, 0);
        renderer.GetActiveCamera().SetFocalPoint(1, 1, 1);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetWindowName("Glyph3DMapper");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "Glyph3DMapper", screenshotPath);
    }
}
