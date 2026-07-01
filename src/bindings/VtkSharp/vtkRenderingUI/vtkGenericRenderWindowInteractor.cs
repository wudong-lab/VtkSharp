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

    public void MouseMoveEvent()
    {
        vtkGenericRenderWindowInteractor_MouseMoveEvent(this.NativePointer);
    }

    public void LeftButtonPressEvent()
    {
        vtkGenericRenderWindowInteractor_LeftButtonPressEvent(this.NativePointer);
    }

    public void LeftButtonReleaseEvent()
    {
        vtkGenericRenderWindowInteractor_LeftButtonReleaseEvent(this.NativePointer);
    }

    public void MiddleButtonPressEvent()
    {
        vtkGenericRenderWindowInteractor_MiddleButtonPressEvent(this.NativePointer);
    }

    public void MiddleButtonReleaseEvent()
    {
        vtkGenericRenderWindowInteractor_MiddleButtonReleaseEvent(this.NativePointer);
    }

    public void RightButtonPressEvent()
    {
        vtkGenericRenderWindowInteractor_RightButtonPressEvent(this.NativePointer);
    }

    public void RightButtonReleaseEvent()
    {
        vtkGenericRenderWindowInteractor_RightButtonReleaseEvent(this.NativePointer);
    }

    public void MouseWheelForwardEvent()
    {
        vtkGenericRenderWindowInteractor_MouseWheelForwardEvent(this.NativePointer);
    }

    public void MouseWheelBackwardEvent()
    {
        vtkGenericRenderWindowInteractor_MouseWheelBackwardEvent(this.NativePointer);
    }

    public void EnterEvent()
    {
        vtkGenericRenderWindowInteractor_EnterEvent(this.NativePointer);
    }

    public void LeaveEvent()
    {
        vtkGenericRenderWindowInteractor_LeaveEvent(this.NativePointer);
    }

    public void KeyPressEvent()
    {
        vtkGenericRenderWindowInteractor_KeyPressEvent(this.NativePointer);
    }

    public void KeyReleaseEvent()
    {
        vtkGenericRenderWindowInteractor_KeyReleaseEvent(this.NativePointer);
    }

    public void CharEvent()
    {
        vtkGenericRenderWindowInteractor_CharEvent(this.NativePointer);
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
    private static extern void vtkGenericRenderWindowInteractor_MouseMoveEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_LeftButtonPressEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_LeftButtonReleaseEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_MiddleButtonPressEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_MiddleButtonReleaseEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_RightButtonPressEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_RightButtonReleaseEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_MouseWheelForwardEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_MouseWheelBackwardEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_EnterEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_LeaveEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_KeyPressEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_KeyReleaseEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_CharEvent(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkGenericRenderWindowInteractor_TimerEventResetsTimerOff(nint self);
    #endregion
}
