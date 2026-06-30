using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkGenericRenderWindowInteractor : vtkRenderWindowInteractor
{
    private vtkGenericRenderWindowInteractor(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) { }

    public new static vtkGenericRenderWindowInteractor New() => new(vtkGenericRenderWindowInteractor_New(), ownsReference: true);

    public new static vtkGenericRenderWindowInteractor WeakReference(nint nativePointer) => new(nativePointer, ownsReference: false);

    public void TimerEvent()
    {
        vtkGenericRenderWindowInteractor_TimerEvent(this.NativePointer);
    }

    public void TimerEventResetsTimerOff()
    {
        vtkGenericRenderWindowInteractor_TimerEventResetsTimerOff(this.NativePointer);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkGenericRenderWindowInteractor_New();

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_TimerEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_TimerEventResetsTimerOff(nint self);
    #endregion
}
