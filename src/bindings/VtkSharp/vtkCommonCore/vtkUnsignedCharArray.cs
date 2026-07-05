namespace VtkSharp;

public partial class vtkUnsignedCharArray
{
    public void InsertNextColor(VtkColor3d color)
    {
        var r = color.R * 255D;
        var g = color.G * 255D;
        var b = color.B * 255D;
        this.InsertNextTuple3(r, g, b); // TODO:
    }

    //public void SetTypedTuple(long tupleIdx, ReadOnlySpan<byte> tuple)
    //{
    //    vtkUnsignedCharArray_SetTypedTuple(this.NativePointer, tupleIdx, tuple);
    //}

    //public void SetNumberOfTuples(long number)
    //{
    //    vtkUnsignedCharArray_SetNumberOfTuples(this.NativePointer, number);
    //}

    //public static vtkUnsignedCharArray CreateRgbColorArray(IReadOnlyCollection<VtkColor3d> rgbColors)
    //{
    //    var array = vtkUnsignedCharArray.New();
    //    array.SetNumberOfComponents(3);
    //    array.SetNumberOfTuples(rgbColors.Count);

    //    var i = 0;
    //    foreach (var color in rgbColors)
    //    {
    //        ReadOnlySpan<byte> rgb =
    //        [
    //            (byte)(color.R * 255D),
    //            (byte)(color.G * 255D),
    //            (byte)(color.B * 255D),
    //        ];
    //        array.SetTypedTuple(i++, rgb);
    //    }

    //    return array;
    //}
}