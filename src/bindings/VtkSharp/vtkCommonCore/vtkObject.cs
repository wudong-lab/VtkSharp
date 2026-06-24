using System.Runtime.InteropServices;

namespace VtkSharp;

public class vtkObject : vtkObjectBase
{
    protected vtkObject(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) { }

    public void Modified()
    {
        vtkObject_Modified(this.NativePointer);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObject_Modified(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern ulong vtkObject_AddObserver(nint self, uint eventId, nint command, float priority);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObject_RemoveObserver(nint self, nint tag);
    #endregion
}