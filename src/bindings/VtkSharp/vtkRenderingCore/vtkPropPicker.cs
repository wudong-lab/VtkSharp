using System.Runtime.InteropServices;

namespace VtkSharp;

public sealed class vtkPropPicker : vtkObject
{
    private vtkPropPicker(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) { }

    public static vtkPropPicker New() => new(vtkPropPicker_New(), ownsReference: true);

    public new static vtkPropPicker WeakReference(nint nativePointer) => new(nativePointer, ownsReference: false);

    public bool Pick(double selectionX, double selectionY, double selectionZ, vtkRenderer renderer)
    {
        return vtkPropPicker_Pick(this.NativePointer, selectionX, selectionY, selectionZ, renderer.NativePointer) != 0;
    }

    public vtkActor? GetActor()
    {
        var actor = vtkPropPicker_GetActor(this.NativePointer);
        return actor == 0 ? null : vtkActor.WeakReference(actor);
    }

    public vtkProp? GetViewProp()
    {
        var prop = vtkPropPicker_GetViewProp(this.NativePointer);
        return prop == 0 ? null : vtkProp.WeakReference(prop);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkPropPicker_New();

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkPropPicker_Pick(
        nint self,
        double selectionX,
        double selectionY,
        double selectionZ,
        nint renderer);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkPropPicker_GetActor(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkPropPicker_GetViewProp(nint self);
    #endregion
}
