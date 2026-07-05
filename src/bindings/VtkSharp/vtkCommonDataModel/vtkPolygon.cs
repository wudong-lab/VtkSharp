using System.Collections.Generic;
using System.Diagnostics;

namespace VtkSharp;

public unsafe partial class vtkPolygon
{
    public void SetVertexPointIds(IReadOnlyCollection<int> vertexPointIds)
    {
        Debug.Assert(vertexPointIds.Count >= 3);
        var pointIds = this.GetPointIds();
        pointIds.SetNumberOfIds(vertexPointIds.Count);

        int i = 0;
        foreach (var id in vertexPointIds)
        {
            pointIds.SetId(i++, id);
        }
    }
}