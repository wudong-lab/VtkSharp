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

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = this.X;
        y = this.Y;
        z = this.Z;
    }

    internal static unsafe VtkVector3d FromPointer(double* vector3d)
    {
        var data = new Span<double>(vector3d, 3);
        return new(data[0], data[1], data[2]);
    }

    /// <summary>
    /// 根据XY平面内的角度构造向量
    /// </summary>
    /// <param name="angle">XY平面内从X轴出发的逆时针旋转角</param>
    /// <returns></returns>
    public static VtkVector3d FromAngleInXyPlane(double angle)
    {
        var pt = VtkPoint3d.Zero.PolarTo(100D, angle);
        return new VtkVector3d(pt.X, pt.Y, 0);
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

    public VtkPoint3d AsPoint3D() => new(this.X, this.Y, this.Z);

    /// <summary>
    /// 特殊的空值，用于表示类似于引用类型的null值
    /// 此特殊值一般仅用于函数无法得到有效的<see cref="VtkVector3d"/>，返回null
    /// </summary>
    public static VtkVector3d Null { get; } = new(double.NaN, double.NaN, double.NaN);

    /// <summary>
    /// 是否为<see cref="VtkVector3d.Null"/>
    /// </summary>
    public bool IsNull => double.IsNaN(this.X) || double.IsNaN(this.Y) || double.IsNaN(this.Z);

    /// <summary>
    /// 是否不为<see cref="VtkVector3d.Null"/>
    /// </summary>
    public bool IsNotNull => !this.IsNull;

    public VtkVector3d Offset(double dx, double dy, double dz) => new(this.X + dx, this.Y + dy, this.Z + dz);
    public VtkVector3d Offset(VtkVector3d offset) => Offset(offset.X, offset.Y, offset.Z);
    public VtkVector3d Offset(VtkVector3d direction, double distance) => Offset(direction * distance);

    public static VtkVector3d operator -(VtkVector3d vector) => new(-vector.X, -vector.Y, -vector.Z);
    public static VtkVector3d operator +(VtkVector3d first, VtkVector3d second) => new(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
    public static VtkVector3d operator *(VtkVector3d vector, double d) => new(vector.X * d, vector.Y * d, vector.Z * d);
    public static VtkVector3d operator *(double d, VtkVector3d vector) => new(vector.X * d, vector.Y * d, vector.Z * d);
    public static VtkVector3d operator /(VtkVector3d vector, double d) => new(vector.X / d, vector.Y / d, vector.Z / d);

    /// <summary>
    /// 向量对应的单位向量
    /// </summary>
    public VtkVector3d UnitVector => this / this.Magnitude;

    /// <summary>
    /// 向量幅值
    /// </summary>
    public double Magnitude => MathEx.Sqrt(this.X, this.Y, this.Z);

    /// <summary>
    /// 当前向量是否为单位向量
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool IsUnitVector(double tolerance = MathEx.Eps)
        => this.Magnitude.EqualTo(1D, tolerance);

    /// <summary>
    /// 根据容差判断向量是否为零向量
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool EqualToZero(double tolerance = MathEx.Eps)
        => this.Magnitude.EqualToZero(tolerance);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool NotEqualToZero(double tolerance = MathEx.Eps)
        => this.Magnitude.NotEqualToZero(tolerance);

    /// <summary>
    /// 
    /// </summary>
    public VtkVector3d Opposite => new(-this.X, -this.Y, -this.Z);

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

    /// <summary>
    /// 判断两向量是否共线（同向或反向）
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool IsParallelOrOppositeTo(VtkVector3d second, double tolerance = MathEx.Eps)
        => CrossProduct(second).Magnitude.EqualToZero(tolerance);

    /// <summary>
    /// 判断两向量是否同向
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool IsParallelTo(VtkVector3d second, double tolerance = MathEx.Eps)
        => IsParallelOrOppositeTo(second, tolerance) && DotProduct(second) > 0;

    /// <summary>
    /// 判断两向量是否反向
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool IsOppositeTo(VtkVector3d second, double tolerance = MathEx.Eps)
        => IsParallelOrOppositeTo(second, tolerance) && DotProduct(second) < 0;

    /// <summary>
    /// 判断两向量是否垂直
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool IsPerpendicularTo(VtkVector3d second, double tolerance = MathEx.Eps)
        => DotProduct(second).EqualToZero(tolerance);

    public bool IsParallelOrOppositeToXAxis(double tolerance = MathEx.Eps)
        => IsParallelOrOppositeTo(UnitX, tolerance);

    public bool IsParallelOrOppositeToYAxis(double tolerance = MathEx.Eps)
        => IsParallelOrOppositeTo(UnitY, tolerance);

    public bool IsParallelOrOppositeToZAxis(double tolerance = MathEx.Eps)
        => IsParallelOrOppositeTo(UnitZ, tolerance);

    /// <summary>
    /// 绕指定轴线向量旋转angle角，得到新的向量
    /// </summary>
    /// <param name="axis">旋转轴向量</param>
    /// <param name="angle">旋转角</param>
    /// <returns></returns>
    public VtkVector3d RotateAroundVector(VtkVector3d axis, double angle)
    {
        //
        // 公式：
        //   P2 = P*cos(angle)+(AxP)sin(angle)+A(A*P)(1-cos(angle))
        // Ref:
        //   https://www.cnblogs.com/wubugui/p/3734627.html
        //
        Debug.Assert(0 <= angle && angle < MathEx.PI2);
        var unitAxis = axis.UnitVector;
        var cost = Math.Cos(angle);
        var sint = Math.Sin(angle);
        return this * cost + unitAxis.CrossProduct(this) * sint +
               unitAxis * unitAxis.DotProduct(this) * (1 - cost);
    }

    public VtkVector3d RotateAroundXAxis(double angle) => RotateAroundVector(UnitX, angle);
    public VtkVector3d RotateAroundYAxis(double angle) => RotateAroundVector(UnitY, angle);
    public VtkVector3d RotateAroundZAxis(double angle) => RotateAroundVector(UnitZ, angle);

    /// <summary>
    /// 求解向量绕normalVector的旋转角，值域[0,2Pi]
    /// </summary>
    /// <param name="second"></param>
    /// <param name="axis">旋转轴</param>
    /// <returns></returns>
    public double AngleWith(VtkVector3d second, VtkVector3d axis)
    {
        if (IsParallelTo(second)) return 0;
        if (IsOppositeTo(second)) return Math.PI;

        var v1 = this.UnitVector;
        var v2 = second.UnitVector;
        var n = v1.CrossProduct(v2);
        Debug.Assert(n.IsParallelOrOppositeTo(axis));
        var theta = Math.Acos(v1.DotProduct(v2));
        return n.IsParallelTo(axis) ? theta : MathEx.PI2 - theta;
    }

    /// <summary>
    /// 根据容差判断两向量是否相等
    /// </summary>
    /// <param name="second"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public bool EqualTo(VtkVector3d second, double tolerance = MathEx.Eps)
        => this.X.EqualTo(second.X, tolerance) && this.Y.EqualTo(second.Y, tolerance) && this.Z.EqualTo(second.Z, tolerance);

    #region Override
    public static bool operator ==(VtkVector3d first, VtkVector3d second) => first.Equals(second);
    public static bool operator !=(VtkVector3d first, VtkVector3d second) => !first.Equals(second);

    /// <summary>
    /// 根据两向量精确值判断是否相等
    /// </summary>
    /// <param name="second"></param>
    /// <returns></returns>
    public bool Equals(VtkVector3d second) => this.X == second.X && this.Y == second.Y && this.Z == second.Z;

    /// <inheritdoc />
    public override bool Equals(object? second) => second is VtkVector3d other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (this.X, this.Y, this.Z).GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"V({this.X:0.###}, {this.Y:0.###}, {this.Z:0.###})";
    #endregion
}