using System;

namespace VtkSharp;

public readonly struct VtkDimension : IEquatable<VtkDimension>
{
    public int Dim1Size { get; }
    public int Dim2Size { get; }
    public int Dim3Size { get; }

    public VtkDimension(int dim1Size, int dim2Size, int dim3Size)
    {
        this.Dim1Size = dim1Size;
        this.Dim2Size = dim2Size;
        this.Dim3Size = dim3Size;
    }

    internal static unsafe VtkDimension FromPointer(int* dimensions)
    {
        var data = new Span<int>(dimensions, 3);
        return new(data[0], data[1], data[2]);
    }

    public bool Equals(VtkDimension other)
        => this.Dim1Size == other.Dim1Size && this.Dim2Size == other.Dim2Size && this.Dim3Size == other.Dim3Size;
}