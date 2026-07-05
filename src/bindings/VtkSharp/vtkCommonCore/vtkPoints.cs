using System.Collections.Generic;

namespace VtkSharp;

public partial class vtkPoints
{
    public long InsertNextPoint(VtkPoint3d point)
        => this.InsertNextPoint(point.X, point.Y, point.Z);

    public static vtkPoints Create(IReadOnlyCollection<VtkPoint3d> points)
    {
        var pts = vtkPoints.New();
        pts.SetNumberOfPoints(points.Count);

        var i = 0;
        foreach (var point in points)
        {
            pts.SetPoint(i++, point.X, point.Y, point.Z);
        }

        return pts;
    }
}