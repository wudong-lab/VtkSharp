using System;
using System.Diagnostics;
using System.Threading;

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

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = this.X;
        y = this.Y;
        z = this.Z;
    }

    internal static unsafe VtkPoint3d FromPointer(double* point3d)
    {
        var data = new Span<double>(point3d, 3);
        return new(data[0], data[1], data[2]);
    }

    public VtkVector3d AsVector3D() => new(this.X, this.Y, this.Z);

    /// <summary>
    /// 零点
    /// </summary>
    public static VtkPoint3d Zero { get; } = new(0, 0, 0);

    /// <summary>
    /// 特殊的空值，用于表示类似于引用类型的null值
    /// 此特殊值一般仅用于函数无法得到有效的<see cref="VtkPoint3d"/>，返回null
    /// </summary>
    public static VtkPoint3d Null { get; } = new(double.NaN, double.NaN, double.NaN);

    /// <summary>
    /// 是否为<see cref="VtkPoint3d.Null"/>
    /// </summary>
    public bool IsNull => double.IsNaN(this.X) || double.IsNaN(this.Y) || double.IsNaN(this.Z);

    /// <summary>
    /// 是否不为<see cref="VtkPoint3d.Null"/>
    /// </summary>
    public bool IsNotNull => !this.IsNull;

    public VtkPoint3d ResetX(double x) => new(x, this.Y, this.Z);
    public VtkPoint3d ResetY(double y) => new(this.X, y, this.Z);
    public VtkPoint3d ResetZ(double z) => new(this.X, this.Y, z);

    /// <summary>
    /// 求两点之间的距离
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double DistanceTo(VtkPoint3d second)
        => MathEx.Sqrt(this.X - second.X, this.Y - second.Y, this.Z - second.Z);

    /// <summary>
    /// 两点X坐标差
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double DeltaXTo(VtkPoint3d second) => this.X - second.X;

    /// <summary>
    /// 两点Y坐标差
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double DeltaYTo(VtkPoint3d second) => this.Y - second.Y;

    /// <summary>
    /// 两点Z坐标差
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double DeltaZTo(VtkPoint3d second) => this.Z - second.Z;

    /// <summary>
    /// 两点X方向距离
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double XDistanceTo(VtkPoint3d second) => Math.Abs(this.X - second.X);

    /// <summary>
    /// 两点Y方向距离
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double YDistanceTo(VtkPoint3d second) => Math.Abs(this.Y - second.Y);

    /// <summary>
    /// 两点Z方向距离
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public double ZDistanceTo(VtkPoint3d second) => Math.Abs(this.Z - second.Z);

    /// <summary>
    /// 求两点之间的中点
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public VtkPoint3d MiddleWith(VtkPoint3d second) => new((this.X + second.X) / 2, (this.Y + second.Y) / 2, (this.Z + second.Z) / 2);

    public VtkPoint3d OffsetX(double dx) => new(this.X + dx, this.Y, this.Z);
    public VtkPoint3d OffsetY(double dy) => new(this.X, this.Y + dy, this.Z);
    public VtkPoint3d OffsetZ(double dz) => new(this.X, this.Y, this.Z + dz);
    public VtkPoint3d Offset(double dx, double dy, double dz = 0) => new(this.X + dx, this.Y + dy, this.Z + dz);

    /// <summary>
    /// 按向量偏移
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public VtkPoint3d Offset(VtkVector3d vector) => Offset(vector.X, vector.Y, vector.Z);

    /// <summary>
    /// 按方向和距离偏移
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="distance">偏移距离</param>
    /// <returns></returns>
    public VtkPoint3d Offset(VtkVector3d direction, double distance) => Offset(direction.UnitVector * distance);

    /// <summary>
    /// 按半径和极角偏移
    /// </summary>
    /// <param name="radius">半径</param>
    /// <param name="angle">从X轴出发的逆时针方向旋转角</param>
    /// <returns></returns>
    public VtkPoint3d PolarTo(double radius, double angle)
    {
        angle = MathEx.NormalizeAngle(angle);
        Debug.Assert(0 <= angle && angle < Math.PI * 2);
        return new VtkPoint3d(this.X + radius * Math.Cos(angle), this.Y + radius * Math.Sin(angle), 0);
    }

    /// <summary>
    /// X坐标反号
    /// </summary>
    public VtkPoint3d OppositeX => new(-this.X, this.Y, this.Z);

    /// <summary>
    /// Y坐标反号
    /// </summary>
    public VtkPoint3d OppositeY => new(this.X, -this.Y, this.Z);

    /// <summary>
    /// Z坐标反号
    /// </summary>
    public VtkPoint3d OppositeZ => new(this.X, this.Y, -this.Z);

    /// <summary>
    /// 当前点指向second点的向量
    /// </summary>
    /// <param name="second"></param>
    /// <returns>两点之间的向量</returns>
    public VtkVector3d VectorTo(VtkPoint3d second)
        => new(second.X - this.X, second.Y - this.Y, second.Z - this.Z);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static VtkPoint3d operator -(VtkPoint3d point)
        => new(-point.X, -point.Y, -point.Z);

    /// <summary>
    /// 两点之间相减，等于第2个点到第1个点的向量
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static VtkVector3d operator -(VtkPoint3d first, VtkPoint3d second)
        => second.VectorTo(first);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="point"></param>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static VtkPoint3d operator +(VtkPoint3d point, VtkVector3d vector)
        => new(point.X + vector.X, point.Y + vector.Y, point.Z + vector.Z);

    /// <summary>
    /// 根据两点距离是否在容差之类判断两点是否等效
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool EqualTo(VtkPoint3d second, double tolerance = MathEx.Eps)
        => this.X.EqualTo(second.X, tolerance) && this.Y.EqualTo(second.Y, tolerance) && this.Z.EqualTo(second.Z, tolerance);

    #region Override
    public static bool operator ==(VtkPoint3d first, VtkPoint3d second) => first.Equals(second);
    public static bool operator !=(VtkPoint3d first, VtkPoint3d second) => !first.Equals(second);

    /// <summary>
    /// 根据两点坐标精确值判断是否相等
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public bool Equals(VtkPoint3d second) => this.X == second.X && this.Y == second.Y && this.Z == second.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VtkPoint3d other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (this.X, this.Y, this.Z).GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"P({this.X:0.###}, {this.Y:0.###}, {this.Z:0.###})";
    #endregion
}