using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkFloatArray
{
    /// <summary>Returns a borrowed pointer to the value at <paramref name="valueIdx"/>.</summary>
    /// <remarks>
    /// The array owns the returned memory. Do not free it, and do not use it after this array is
    /// disposed or an operation that can reallocate its storage.
    /// </remarks>
    public float* GetPointer(long valueIdx)
        => vtkFloatArray_GetPointer(this.NativePointer, valueIdx);

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern float* vtkFloatArray_GetPointer(nint self, long valueIdx);
    #endregion
}
