using System;
using System.Diagnostics;

namespace VtkSharp;

public readonly struct VtkVector3d
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public VtkVector3d(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    internal static unsafe VtkVector3d FromPointer(double* vector3d)
    {
        var data = new Span<double>(vector3d, 3);
        return new(data[0], data[1], data[2]);
    }

    /// <summary>
    /// 零向量
    /// </summary>
    public static VtkVector3d Zero { get; } = new(0, 0, 0);

    /// <summary>
    /// X方向单位向量
    /// </summary>
    public static VtkVector3d UnitX { get; } = new(1D, 0, 0);

    /// <summary>
    /// Y方向单位向量
    /// </summary>
    public static VtkVector3d UnitY { get; } = new(0, 1D, 0);

    /// <summary>
    /// Z方向单位向量
    /// </summary>
    public static VtkVector3d UnitZ { get; } = new(0, 0, 1D);

    /// <summary>
    /// 向量对应的单位向量
    /// </summary>
    public VtkVector3d UnitVector
    {
        get
        {
            var magnitude = this.Magnitude;
            Debug.Assert(Math.Abs(magnitude) > 1e-12);
            return new VtkVector3d(this.X / magnitude, this.Y / magnitude, this.Z / magnitude);
        }
    }

    /// <summary>
    /// 向量幅值
    /// </summary>
    public double Magnitude
        => Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);

    /// <summary>
    /// 向量点积
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double DotProduct(VtkVector3d second)
        => this.X * second.X + this.Y * second.Y + this.Z * second.Z;

    /// <summary>
    /// 向量叉积
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public VtkVector3d CrossProduct(VtkVector3d second)
        => new(this.Y * second.Z - this.Z * second.Y, this.Z * second.X - this.X * second.Z, this.X * second.Y - this.Y * second.X);
}