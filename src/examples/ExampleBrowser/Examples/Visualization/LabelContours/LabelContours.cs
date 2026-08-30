using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("LabelContours", "Visualization",
    Description = "Labels contour polylines on a random scalar field.",
    SourceFiles = new[] { "Examples/Visualization/LabelContours/LabelContours.cs" })]
internal sealed class LabelContours : ISmokeExample
{
    public void Run()
    {
        // 将等值线片段拼接为折线，并在每条折线上选择点标注标量值。
        // https://examples.vtk.org/site/Cxx/Visualization/LabelContours/
        Render(null);
    }

    public void RenderScreenshot(string screenshotPath) => Render(screenshotPath);

    private static void Render(string? screenshotPath)
    {
        using var randomSequence = vtkMinimalStandardRandomSequence.New();
        randomSequence.SetSeed(1);

        using var plane = vtkPlaneSource.New();
        plane.SetResolution(10, 10);
        plane.Update();
        using var polyData = plane.GetOutput();
        using var randomScalars = vtkDoubleArray.New();
        randomScalars.SetNumberOfComponents(1);
        randomScalars.SetName("Isovalues");
        for (long i = 0; i < polyData.GetNumberOfPoints(); i++)
        {
            randomScalars.InsertNextTuple1(randomSequence.GetRangeValue(-100.0, 100.0));
            randomSequence.Next();
        }
        polyData.GetPointData().SetScalars(randomScalars);

        using var contours = vtkContourFilter.New();
        contours.SetInputConnection(plane.GetOutputPort());
        contours.GenerateValues(5, -100.0, 100.0);
        using var contourStripper = vtkStripper.New();
        contourStripper.SetInputConnection(contours.GetOutputPort());
        contourStripper.Update();
        using var contourData = contourStripper.GetOutput();
        Debug.WriteLine($"There are {contourData.GetNumberOfLines()} contour lines.");
        using var points = contourData.GetPoints();
        using var cells = contourData.GetLines();
        using var scalars = contourData.GetPointData().GetScalars();

        using var labelPolyData = vtkPolyData.New();
        using var labelPoints = vtkPoints.New();
        using var labelScalars = vtkDoubleArray.New();
        labelScalars.SetNumberOfComponents(1);
        labelScalars.SetName("Isovalues");
        using var cellIter = cells.NewIterator();
        Span<double> labelPoint = stackalloc double[3];
        for (cellIter.GoToFirstCell(); !cellIter.IsDoneWithTraversal(); cellIter.GoToNextCell())
        {
            using var cell = cellIter.GetCurrentCell();
            // 也可使用 cell.GetNumberOfIds() / 2，选取点序列中间的点（非弧长中点）。
            var samplePointIndex = (long)randomSequence.GetRangeValue(0, cell.GetNumberOfIds());
            randomSequence.Next();
            var pointId = cell.GetId(samplePointIndex);
            points.GetPoint(pointId, labelPoint);
            var value = scalars.GetTuple1(pointId);
            Debug.WriteLine($"Line {cellIter.GetCurrentCellId()}: point {pointId}, value {value:F2}");
            labelPoints.InsertNextPoint(labelPoint);
            labelScalars.InsertNextTuple1(value);
        }
        labelPolyData.SetPoints(labelPoints);
        labelPolyData.GetPointData().SetScalars(labelScalars);

        using var contourMapper = vtkPolyDataMapper.New();
        contourMapper.SetInputConnection(contourStripper.GetOutputPort());
        contourMapper.ScalarVisibilityOff();
        using var isolines = vtkActor.New();
        isolines.SetMapper(contourMapper);
        isolines.GetProperty().SetColor(VtkColor3d.Black);
        isolines.GetProperty().SetLineWidth(2);

        Span<double> range = stackalloc double[2];
        polyData.GetScalarRange(range);
        using var surfaceLut = vtkLookupTable.New();
        surfaceLut.SetRange(range);
        surfaceLut.Build();
        using var surfaceMapper = vtkPolyDataMapper.New();
        surfaceMapper.SetInputData(polyData);
        surfaceMapper.ScalarVisibilityOn();
        surfaceMapper.SetScalarRange(range);
        surfaceMapper.SetLookupTable(surfaceLut);
        using var surface = vtkActor.New();
        surface.SetMapper(surfaceMapper);

        using var labelMapper = vtkLabeledDataMapper.New();
        labelMapper.SetFieldDataName("Isovalues");
        labelMapper.SetInputData(labelPolyData);
        labelMapper.SetLabelModeToLabelScalars();
        labelMapper.SetLabelFormat("{:6.2f}");
        var gold = VtkColor3d.Gold;
        labelMapper.GetLabelTextProperty().SetColor(gold.R, gold.G, gold.B);
        using var isolabels = vtkActor2D.New();
        isolabels.SetMapper(labelMapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(surface);
        renderer.AddActor(isolines);
        renderer.AddActor(isolabels);
        renderer.SetBackground(VtkColor3d.DarkSlateGray);
        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(600, 600);
        window.SetWindowName("LabelContours");
        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);
        window.Render();
        if (screenshotPath is not null)
        {
            using var image = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }
        interactor.Start();
    }
}
