using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkCellArray
{
    public long AddLine(long startPointIndex, long endPointIndex)
    {
        using var line = vtkLine.New();
        var pointIds = line.GetPointIds();
        pointIds.SetId(0, startPointIndex);
        pointIds.SetId(1, endPointIndex);

        return this.InsertNextCell(line);
    }

    public long AddQuad(long vertexIndex1, long vertexIndex2, long vertexIndex3, long vertexIndex4)
    {
        using var quad = vtkQuad.New();
        var pointIds = quad.GetPointIds();
        pointIds.SetId(0, vertexIndex1);
        pointIds.SetId(1, vertexIndex2);
        pointIds.SetId(2, vertexIndex3);
        pointIds.SetId(3, vertexIndex4);

        return this.InsertNextCell(quad);
    }

    public long AddPolygon(IReadOnlyCollection<long> vertexIndices)
    {
        Debug.Assert(vertexIndices.Count >= 3);

        using var polygon = vtkPolygon.New();
        var pointIds = polygon.GetPointIds();
        pointIds.SetNumberOfIds(vertexIndices.Count);

        var i = 0;
        foreach (var index in vertexIndices)
        {
            pointIds.SetId(i++, index);
        }

        return this.InsertNextCell(polygon);
    }

    public long AddPolyLine(IReadOnlyCollection<long> vertexIndices)
    {
        Debug.Assert(vertexIndices.Count >= 2);

        using var polygon = vtkPolyLine.New();
        var pointIds = polygon.GetPointIds();
        pointIds.SetNumberOfIds(vertexIndices.Count);

        var i = 0;
        foreach (var index in vertexIndices)
        {
            pointIds.SetId(i++, index);
        }

        return this.InsertNextCell(polygon);
    }
}