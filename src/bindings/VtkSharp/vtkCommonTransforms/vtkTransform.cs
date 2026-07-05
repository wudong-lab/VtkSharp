using System;
using System.Diagnostics;

namespace VtkSharp;

public unsafe partial class vtkTransform
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="zAxis"></param>
    /// <param name="xAxis"></param>
    public void SetLocalToWorld(VtkPoint3d origin, VtkVector3d zAxis, VtkVector3d xAxis)
    {
        Debug.Assert(zAxis.Magnitude > 0);
        Debug.Assert(xAxis.Magnitude > 0);

        //
        // https://www.semath.info/src/inverse-cofactor-ex4.html
        //
        var ex = xAxis.UnitVector;
        var ez = zAxis.UnitVector;
        var ey = ez.CrossProduct(ex);

        Span<double> data =
        [
            ex.X, ey.X, ez.X, origin.X,
            ex.Y, ey.Y, ez.Y, origin.Y,
            ex.Z, ey.Z, ez.Z, origin.Z,
            0, 0, 0, 1,
        ];
        this.SetMatrix(data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="zAxis"></param>
    /// <param name="xAxis"></param>
    public void SetWorldToLocal(VtkPoint3d origin, VtkVector3d zAxis, VtkVector3d xAxis)
    {
        Debug.Assert(zAxis.Magnitude > 0);
        Debug.Assert(xAxis.Magnitude > 0);

        //
        // https://www.semath.info/src/inverse-cofactor-ex4.html
        //
        var ex = xAxis.UnitVector;
        var ez = zAxis.UnitVector;
        var ey = ez.CrossProduct(ex);

        Span<double> data =
        [
            ex.X, ey.X, ez.X, origin.X,
            ex.Y, ey.Y, ez.Y, origin.Y,
            ex.Z, ey.Z, ez.Z, origin.Z,
            0, 0, 0, 1,
        ];

        using var matrix = vtkMatrix4x4.New();
        matrix.SetData(data);
        matrix.Invert();

        this.SetMatrix(matrix);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public VtkPoint3d TransformPoint(VtkPoint3d point)
    {
        Span<double> input = [point.X, point.Y, point.Z];
        Span<double> output = [0, 0, 0];
        this.TransformPoint(input, output);
        return new VtkPoint3d(output[0], output[1], output[2]);
    }
}