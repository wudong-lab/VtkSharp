using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of VTK/Examples/GeometricObjects/Cxx/Circle.cxx
// Creates a circle (outline polygon with 50 sides) using vtkRegularPolygonSource
// and renders it in cornsilk color on a dark green background.

internal class Circle
{
    static void Main()
    {
        using var colors = vtkNamedColors.New();

        using var polygonSource = vtkRegularPolygonSource.New();
        polygonSource.GeneratePolygonOff();
        polygonSource.SetNumberOfSides(50);
        polygonSource.SetRadius(5.0);
        polygonSource.SetCenter(0.0, 0.0, 0.0);

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(polygonSource.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        Span<double> cornsilk = stackalloc double[3];
        colors.GetColorRGB("Cornsilk", cornsilk);
        actor.GetProperty().SetColor(cornsilk[0], cornsilk[1], cornsilk[2]);

        Span<double> darkGreen = stackalloc double[3];
        colors.GetColorRGB("DarkGreen", darkGreen);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);
        renderer.SetBackground(darkGreen[0], darkGreen[1], darkGreen[2]);

        using var renderWindow = vtkRenderWindow.New();
        renderWindow.AddRenderer(renderer);

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetRenderWindow(renderWindow);

        renderWindow.SetWindowName("Circle");
        renderWindow.Render();
        Console.WriteLine("Circle example running. Close the window to exit.");
        renderWindowInteractor.Start();
    }
}
