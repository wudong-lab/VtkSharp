using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("PolyLine", "GeometricObjects",
    Description = "Creates a polyline connecting five 3D points and renders it as a Tomato-colored line.",
    SourceFiles = new[] { "Examples/GeometricObjects/PolyLine/PolyLine.cs" })]
internal class PolyLine : IExample
{
    public void Run()
    {
        using var colors = vtkNamedColors.New();

        // Create five points.
        // Create a vtkPoints object and store the points in it.
        using var points = vtkPoints.New();
        points.InsertNextPoint(0.0, 0.0, 0.0);
        points.InsertNextPoint(1.0, 0.0, 0.0);
        points.InsertNextPoint(0.0, 1.0, 0.0);
        points.InsertNextPoint(0.0, 1.0, 2.0);
        points.InsertNextPoint(1.0, 2.0, 3.0);

        using var polyLine = vtkPolyLine.New();
        var polyLinePointIds = polyLine.GetPointIds(); // not owned by caller
        polyLinePointIds.SetNumberOfIds(5);
        for (int i = 0; i < 5; i++)
        {
            polyLinePointIds.SetId(i, i);
        }

        // Create a cell array to store the lines in and add the lines to it.
        using var cells = vtkCellArray.New();
        cells.InsertNextCell(polyLine);

        // Create a polydata to store everything in.
        using var polyData = vtkPolyData.New();

        // Add the points to the dataset.
        polyData.SetPoints(points);

        // Add the lines to the dataset.
        polyData.SetLines(cells);

        // Setup actor and mapper.
        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputData(polyData);

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        var tomato = colors.GetColor3d("Tomato");
        actor.GetProperty().SetColor(tomato.R, tomato.G, tomato.B);

        // Setup render window, renderer, and interactor.
        using var renderer = vtkRenderer.New();
        using var renderWindow = vtkRenderWindow.New();
        renderWindow.SetWindowName("PolyLine");
        renderWindow.AddRenderer(renderer);
        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);
        renderer.AddActor(actor);
        var bg = colors.GetColor3d("DarkOliveGreen");
        renderer.SetBackground(bg.R, bg.G, bg.B);

        renderWindow.Render();
        Debug.WriteLine("PolyLine example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
