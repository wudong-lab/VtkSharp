using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ColoredLines", "GeometricObjects",
    Description = "Builds a two-member frame and assigns an independent color to each element.",
    SourceFiles = new[] { "Examples/GeometricObjects/ColoredLines/ColoredLines.cs" })]
internal class ColoredLines : ISmokeExample
{
    public void Run() => Render(null);
    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        // Original C++ example displays two differently colored lines:
        // https://examples.vtk.org/site/Cxx/GeometricObjects/ColoredLines/
        using var colors = vtkNamedColors.New();
        using var points = vtkPoints.New();
        points.InsertNextPoint(0, 0, 0);
        points.InsertNextPoint(1, 0, 0);
        points.InsertNextPoint(0, 1, 0);

        using var lines = vtkCellArray.New();
        using var firstMember = vtkLine.New();
        firstMember.GetPointIds().SetId(0, 0);
        firstMember.GetPointIds().SetId(1, 1);
        lines.InsertNextCell(firstMember);
        using var secondMember = vtkLine.New();
        secondMember.GetPointIds().SetId(0, 0);
        secondMember.GetPointIds().SetId(1, 2);
        lines.InsertNextCell(secondMember);

        using var elementColors = vtkUnsignedCharArray.New();
        elementColors.SetNumberOfComponents(3);
        elementColors.SetNumberOfTuples(2);
        var tomato = colors.GetColor3ub("Tomato");
        var mint = colors.GetColor3ub("Mint");
        elementColors.SetTuple3(0, tomato.R, tomato.G, tomato.B);
        elementColors.SetTuple3(1, mint.R, mint.G, mint.B);

        using var frame = vtkPolyData.New();
        frame.SetPoints(points);
        frame.SetLines(lines);
        frame.GetCellData().SetScalars(elementColors);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputData(frame);
        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetLineWidth(4);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        var background = colors.GetColor3d("SlateGray");
        renderer.SetBackground(background.R, background.G, background.B);
        using var window = vtkRenderWindow.New();
        window.SetWindowName("Colored frame members");
        window.AddRenderer(renderer);
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        ExampleRenderSupport.Finish(window, interactor, "ColoredLines", screenshotPath);
    }
}
