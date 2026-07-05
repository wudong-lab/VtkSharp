using System;
using System.Diagnostics;
using System.Linq;

namespace VtkSharp;

/// <summary>
/// 数学计算辅助函数
/// </summary>
internal static class MathEx
{
    /// <summary>
    /// 用于浮点数比较的默认容差
    /// </summary>
    public const double Eps = 1e-6;

    /// <summary>
    /// π常数
    /// </summary>
    public const double PI = Math.PI;

    /// <summary>
    /// 2π
    /// </summary>
    public const double PI2 = Math.PI * 2D;

    /// <summary>
    /// 4π
    /// </summary>
    public const double PI4 = Math.PI * 4D;

    /// <summary>
    /// π/2
    /// </summary>
    public const double PI_2 = Math.PI / 2D;

    /// <summary>
    /// π/4
    /// </summary>
    public const double PI_4 = Math.PI / 4D;

    /// <summary>
    /// 基于容差判断两个浮点数是否相等
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance">容差</param>
    /// <returns></returns>
    public static bool EqualTo(this double x, double y, double tolerance = Eps)
        => Math.Abs(x - y) <= tolerance;

    /// <summary>
    /// 基于容差判断两个浮点数是否不相等
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool NotEqualTo(this double x, double y, double tolerance = Eps)
        => !x.EqualTo(y, tolerance);

    /// <summary>
    /// 基于容差判断是否x大于等于y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool GreaterOrEqualTo(this double x, double y, double tolerance = Eps)
        => x > y || Math.Abs(x - y) <= tolerance;

    /// <summary>
    /// 基于容差判断是否x小于等于y
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool LessOrEqualTo(this double x, double y, double tolerance = Eps)
        => x < y || Math.Abs(x - y) <= tolerance;

    /// <summary>
    /// 基于容差判断是否x大于等于0
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool GreaterOrEqualToZero(this double x, double tolerance = Eps)
        => x > 0 || Math.Abs(x) <= tolerance;

    /// <summary>
    /// 基于容差判断是否x小于等于0
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool LessOrEqualToZero(this double x, double tolerance = Eps)
        => x < 0 || Math.Abs(x) <= tolerance;

    /// <summary>
    /// 基于容差判断浮点数是否等于零
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance">容差</param>
    /// <returns></returns>
    public static bool EqualToZero(this double x, double tolerance = Eps)
        => Math.Abs(x) <= tolerance;

    /// <summary>
    /// 基于容差判断浮点数是否不等于零
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance">容差</param>
    /// <returns></returns>
    public static bool NotEqualToZero(this double x, double tolerance = Eps)
        => !x.EqualToZero(tolerance);

    /// <summary>
    /// 基于容差判断浮点数是否等于零
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool EqualToZero(this float x, double tolerance = Eps)
        => Math.Abs(x) <= tolerance;

    /// <summary>
    /// 基于容差判断浮点数是否不等于零
    /// </summary>
    /// <param name="x"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static bool NotEqualToZero(this float x, double tolerance = Eps)
        => !x.EqualToZero(tolerance);

    /// <summary>
    /// 求平方和，x1^2+x2^2
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <returns></returns>
    public static double SquareSum(double x1, double x2) => x1 * x1 + x2 * x2;

    /// <summary>
    /// 求平方和，x1^2+x2^2+x3^3
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <param name="x3"></param>
    /// <returns></returns>
    public static double SquareSum(double x1, double x2, double x3) => x1 * x1 + x2 * x2 + x3 * x3;

    /// <summary>
    /// 平方和，x1^2+x2^2+ ... +xn^3
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double SquareSum(params double[] xs) => xs.Sum(x => x * x);

    /// <summary>
    /// 平方和开方
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double Sqrt(params double[] xs) => Math.Sqrt(SquareSum(xs));

    /// <summary>
    /// 平方和开方
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <returns></returns>
    public static double Sqrt(double x1, double x2) => Math.Sqrt(x1 * x1 + x2 * x2);

    /// <summary>
    /// 平方和开方
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <param name="x3"></param>
    /// <returns></returns>
    public static double Sqrt(double x1, double x2, double x3) => Math.Sqrt(x1 * x1 + x2 * x2 + x3 * x3);

    /// <summary>
    /// 角度值转化为弧度值
    /// </summary>
    /// <param name="degree">角度</param>
    /// <returns>弧度</returns>
    public static double DegToRad(this double degree)
        => degree * PI / 180D;

    /// <summary>
    /// 弧度值转化为角度值
    /// </summary>
    /// <param name="radian">弧度</param>
    /// <returns>角度</returns>
    public static double RadToDeg(this double radian)
        => radian * 180D / PI;

    /// <summary>
    /// 将角度正规化到 [0, 2π) 区间
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static double NormalizeAngle(double angle)
        => angle - Math.Floor(angle / PI2) * PI2;

    /// <summary>
    /// 判断X是否在 [x1, x2] 区间
    /// </summary>
    /// <param name="x"></param>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <returns></returns>
    public static bool InRange(this double x, double x1, double x2)
        => (x - x1) * (x - x2) <= 0;

    /// <summary>
    /// 线性插值
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public static double Interpolate(this double x1, double x2, double ratio)
        => x1 + (x2 - x1) * ratio;

    /// <summary>
    /// 绝对值的最大值
    /// </summary>
    /// <param name="xs"></param>
    /// <returns></returns>
    public static double AbsMax(params double[] xs)
        => xs.Max(Math.Abs);

    /// <summary>
    /// 比较绝对值相对大小，返回绝对值最大的值（原始值）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static double MaxForAbs(double x, double y)
        => Math.Abs(x) >= Math.Abs(y) ? x : y;

    /// <summary>
    /// 比较绝对值相对大小，返回绝对值最小的值（原始值）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static double MinForAbs(double x, double y)
        => Math.Abs(x) <= Math.Abs(y) ? x : y;

    /// <summary>
    /// 当X小于lowerBound时，取lowerBound；当x大于upperBound时，取upperBound；否则返回x
    /// </summary>
    /// <param name="x"></param>
    /// <param name="lowerBound"></param>
    /// <param name="upperBound"></param>
    /// <returns></returns>
    public static double Clamp(double x, double lowerBound, double upperBound)
    {
        Debug.Assert(lowerBound < upperBound);
        if (x < lowerBound) return lowerBound;
        if (x > upperBound) return upperBound;
        return x;
    }

    /// <summary>
    /// 求圆的面积
    /// </summary>
    /// <param name="diameter">直径</param>
    /// <returns></returns>
    public static double GetCircleArea(double diameter)
    {
        Debug.Assert(diameter > 0);
        const double pi_4 = Math.PI / 4;
        return pi_4 * diameter * diameter;
    }

    #region 三角函数
    /// <summary>
    /// 根据三角形的三条边长，求c边对角的角度
    /// </summary>
    /// <param name="a">a边长</param>
    /// <param name="b">b边长</param>
    /// <param name="c">c边长</param>
    /// <returns></returns>
    public static double CosinesFormula(double a, double b, double c)
    {
        Debug.Assert(a > 0 && b > 0 && c > 0);
        Debug.Assert(a + b > c && b + c > a && c + a > b);

        //
        // 三角形余弦公式：
        //
        //         a^2+b^2-c^2
        // cosC = -------------
        //             2ab
        //
        var cosc = (a * a + b * b - c * c) / (2 * a * b);
        return Math.Acos(cosc);
    }
    #endregion
}