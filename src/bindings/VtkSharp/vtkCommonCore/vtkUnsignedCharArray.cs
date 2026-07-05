using System;
using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkUnsignedCharArray
{
    public void InsertNextColor(VtkColor3d color)
    {
        var r = color.R * 255D;
        var g = color.G * 255D;
        var b = color.B * 255D;
        this.InsertNextTuple3(r, g, b); // TODO:
    }

    public void SetUnsignedCharTuple(long tupleIdx, ReadOnlySpan<byte> unsignedCharTuple)
    {
        fixed (byte* tuplePtr = unsignedCharTuple)
        {
            vtkUnsignedCharArray_SetUnsignedCharTuple(this.NativePointer, tupleIdx, tuplePtr);
        }
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkUnsignedCharArray_SetUnsignedCharTuple(nint self, long tupleIdx, byte* tuple);
    #endregion
}