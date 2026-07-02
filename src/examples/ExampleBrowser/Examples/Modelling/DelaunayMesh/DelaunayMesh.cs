using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("DelaunayMesh", "Modelling",
    Description = "Demonstrates 2D Delaunay triangulation with tube-wrapped edges and sphere glyphs at points.",
    SourceFiles = new[] { "Examples/Modelling/DelaunayMesh/DelaunayMesh.cs" })]
internal class DelaunayMesh : IExample
{
    public void Run()
    {
        using var colors = vtkNamedColors.New();

        // Generate some "random" points.
        using var points = vtkPoints.New();
        using var randomSequence = vtkMinimalStandardRandomSequence.New();
        randomSequence.SetSeed(1);
        for (int i = 0; i < 50; ++i)
        {
            double p1 = randomSequence.GetValue();
            randomSequence.Next();
            double p2 = randomSequence.GetValue();
            randomSequence.Next();
            points.InsertPoint(i, p1, p2, 0.0);
        }

        // Create a polydata with the points we just created.
        using var profile = vtkPolyData.New();
        profile.SetPoints(points);

        // Perform a 2D Delaunay triangulation on them.
        using var delny = vtkDelaunay2D.New();
        delny.SetInputData(profile);
        delny.SetTolerance(0.001);

        using var mapMesh = vtkPolyDataMapper.New();
        mapMesh.SetInputConnection(delny.GetOutputPort());

        using var meshActor = vtkActor.New();
        meshActor.SetMapper(mapMesh);
        var midnightBlue = colors.GetColor3d("MidnightBlue");
        meshActor.GetProperty().SetColor(midnightBlue.R, midnightBlue.G, midnightBlue.B);

        // Wrap the edges in tubes, and put fat spheres at the points.
        using var extract = vtkExtractEdges.New();
        extract.SetInputConnection(delny.GetOutputPort());

        using var tubes = vtkTubeFilter.New();
        tubes.SetInputConnection(extract.GetOutputPort());
        tubes.SetRadius(0.01);
        tubes.SetNumberOfSides(6);

        using var mapEdges = vtkPolyDataMapper.New();
        mapEdges.SetInputConnection(tubes.GetOutputPort());

        using var edgeActor = vtkActor.New();
        edgeActor.SetMapper(mapEdges);
        var peacock = colors.GetColor3d("peacock");
        edgeActor.GetProperty().SetColor(peacock.R, peacock.G, peacock.B);
        edgeActor.GetProperty().SetSpecularColor(1, 1, 1);
        edgeActor.GetProperty().SetSpecular(0.3);
        edgeActor.GetProperty().SetSpecularPower(20);
        edgeActor.GetProperty().SetAmbient(0.2);
        edgeActor.GetProperty().SetDiffuse(0.8);

        using var ball = vtkSphereSource.New();
        ball.SetRadius(0.025);
        ball.SetThetaResolution(12);
        ball.SetPhiResolution(12);

        using var balls = vtkGlyph3D.New();
        balls.SetInputConnection(delny.GetOutputPort());
        balls.SetSourceConnection(ball.GetOutputPort());

        using var mapBalls = vtkPolyDataMapper.New();
        mapBalls.SetInputConnection(balls.GetOutputPort());

        using var ballActor = vtkActor.New();
        ballActor.SetMapper(mapBalls);
        var hotPink = colors.GetColor3d("hot_pink");
        ballActor.GetProperty().SetColor(hotPink.R, hotPink.G, hotPink.B);
        ballActor.GetProperty().SetSpecularColor(1, 1, 1);
        ballActor.GetProperty().SetSpecular(0.3);
        ballActor.GetProperty().SetSpecularPower(20);
        ballActor.GetProperty().SetAmbient(0.2);
        ballActor.GetProperty().SetDiffuse(0.8);

        // Create the rendering window, renderer, and interactive renderer.
        using var ren = vtkRenderer.New();
        using var renWin = vtkRenderWindow.New();
        renWin.AddRenderer(ren);
        using var iren = vtkRenderWindowInteractor.New();
        iren.SetRenderWindow(renWin);

        // Add the actors to the renderer, set the background and size.
        ren.AddActor(meshActor);
        ren.AddActor(ballActor);
        ren.AddActor(edgeActor);
        var aliceBlue = colors.GetColor3d("AliceBlue");
        ren.SetBackground(aliceBlue.R, aliceBlue.G, aliceBlue.B);
        renWin.SetSize(512, 512);
        renWin.SetWindowName("DelaunayMesh");

        ren.ResetCamera();
        ren.GetActiveCamera().Zoom(1.3);

        // Interact with the data.
        renWin.Render();
        iren.Start();
    }
}
