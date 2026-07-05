using System.Collections.Generic;
using System.Diagnostics;

namespace VtkSharp;

public unsafe partial class vtkPolyData
{
    public static vtkPolyData CreatePolyLine(IReadOnlyCollection<VtkPoint3d> points, bool isClosed)
    {
        Debug.Assert(points.Count >= 2);

        using var vtkPoints = VtkSharp.vtkPoints.New();
        vtkPoints.SetNumberOfPoints(points.Count);
        var i = 0;
        foreach (var point in points)
        {
            vtkPoints.SetPoint(i++, point.X, point.Y, point.Z);
        }

        using var polyLine = vtkPolyLine.New();
        var pointIds = polyLine.GetPointIds();
        var targetPointCount = isClosed ? points.Count + 1 : points.Count;
        pointIds.SetNumberOfIds(targetPointCount);

        for (var j = 0; j < points.Count; ++j)
        {
            pointIds.SetId(j, j);
        }

        if (isClosed)
        {
            pointIds.SetId(points.Count, 0);
        }

        using var cellArray = vtkCellArray.New();
        cellArray.InsertNextCell(polyLine);

        var polyData = vtkPolyData.New();
        polyData.SetPoints(vtkPoints);
        polyData.SetLines(cellArray);

        return polyData;
    }

    public static vtkPolyData CreatePolygon(IReadOnlyCollection<VtkPoint3d> points)
    {
        Debug.Assert(points.Count >= 3);

        using var vtkPoints = VtkSharp.vtkPoints.New();
        vtkPoints.SetNumberOfPoints(points.Count);
        var i = 0;
        foreach (var point in points)
        {
            vtkPoints.SetPoint(i++, point.X, point.Y, point.Z);
        }

        using var polygon = vtkPolygon.New();
        var pointIds = polygon.GetPointIds();
        pointIds.SetNumberOfIds(points.Count);

        for (var j = 0; j < points.Count; ++j)
        {
            pointIds.SetId(j, j);
        }

        using var cellArray = vtkCellArray.New();
        cellArray.InsertNextCell(polygon);

        var polyData = vtkPolyData.New();
        polyData.SetPoints(vtkPoints);
        polyData.SetPolys(cellArray);

        return polyData;
    }

    public static vtkPolyData CreateQuad(VtkPoint3d point1, VtkPoint3d point2, VtkPoint3d point3, VtkPoint3d point4)
    {
        using var vtkPoints = VtkSharp.vtkPoints.New();
        vtkPoints.SetNumberOfPoints(4);
        vtkPoints.SetPoint(0, point1.X, point1.Y, point1.Z);
        vtkPoints.SetPoint(1, point2.X, point2.Y, point2.Z);
        vtkPoints.SetPoint(2, point3.X, point3.Y, point3.Z);
        vtkPoints.SetPoint(3, point4.X, point4.Y, point4.Z);

        using var quad = vtkQuad.New();
        var pointIds = quad.GetPointIds();
        pointIds.SetId(0, 0);
        pointIds.SetId(1, 1);
        pointIds.SetId(2, 2);
        pointIds.SetId(3, 3);

        using var cellArray = vtkCellArray.New();
        cellArray.InsertNextCell(quad);

        var polyData = vtkPolyData.New();
        polyData.SetPoints(vtkPoints);
        polyData.SetPolys(cellArray);

        return polyData;
    }

    public static vtkPolyData MergePolyData(IEnumerable<vtkPolyData> polyDatas)
    {
        using var appendPolyData = vtkAppendPolyData.New();
        foreach (var polyData in polyDatas)
        {
            appendPolyData.AddInputData(polyData);
        }

        appendPolyData.Update();

        var resultPolyData = appendPolyData.GetOutput();
        return vtkPolyData.Register(resultPolyData);
    }
}