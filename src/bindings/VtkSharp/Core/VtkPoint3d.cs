using System;

namespace VtkSharp;

public readonly struct VtkPoint3d
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public VtkPoint3d(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    internal static unsafe VtkPoint3d FromPointer(double* point3d)
    {
        var data = new Span<double>(point3d, 3);
        return new(data[0], data[1], data[2]);
    }
}