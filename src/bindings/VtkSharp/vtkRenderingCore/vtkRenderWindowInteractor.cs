using System;
using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindowInteractor
{
    public void GetEventPosition(out int x, out int y)
    {
        int* position = stackalloc int[2];
        vtkRenderWindowInteractor_GetEventPosition(this.NativePointer, position);
        x = position[0];
        y = position[1];
    }

    public void GetLastEventPosition(out int x, out int y)
    {
        int* position = stackalloc int[2];
        vtkRenderWindowInteractor_GetLastEventPosition(this.NativePointer, position);
        x = position[0];
        y = position[1];
    }

    public bool GetControlKey() => vtkRenderWindowInteractor_GetControlKey(this.NativePointer) != 0;
    public bool GetShiftKey() => vtkRenderWindowInteractor_GetShiftKey(this.NativePointer) != 0;
    public bool GetAltKey() => vtkRenderWindowInteractor_GetAltKey(this.NativePointer) != 0;
    public char GetKeyCode() => (char)vtkRenderWindowInteractor_GetKeyCode(this.NativePointer);
    public string GetKeySym() => VtkString.FromUtf8Pointer(vtkRenderWindowInteractor_GetKeySym(this.NativePointer));
    public int GetRepeatCount() => vtkRenderWindowInteractor_GetRepeatCount(this.NativePointer);

    public VtkObserverHandle AddMouseMoveEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.MouseMoveEvent, callback, priority);
    }

    public VtkObserverHandle AddLeftButtonPressEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.LeftButtonPressEvent, callback, priority);
    }

    public VtkObserverHandle AddLeftButtonReleaseEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.LeftButtonReleaseEvent, callback, priority);
    }

    public VtkObserverHandle AddRightButtonPressEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.RightButtonPressEvent, callback, priority);
    }

    public VtkObserverHandle AddRightButtonReleaseEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.RightButtonReleaseEvent, callback, priority);
    }

    public VtkObserverHandle AddMouseWheelForwardEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.MouseWheelForwardEvent, callback, priority);
    }

    public VtkObserverHandle AddMouseWheelBackwardEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(VtkCommandEventIds.MouseWheelBackwardEvent, callback, priority);
    }

    public VtkObserverHandle AddKeyPressEventObserver(Action<VtkKeyEventArgs> callback, float priority = 0.0f)
    {
        return this.AddKeyObserver(VtkCommandEventIds.KeyPressEvent, callback, priority);
    }

    public VtkObserverHandle AddKeyReleaseEventObserver(Action<VtkKeyEventArgs> callback, float priority = 0.0f)
    {
        return this.AddKeyObserver(VtkCommandEventIds.KeyReleaseEvent, callback, priority);
    }

    private VtkObserverHandle AddMouseObserver(uint eventId, Action<VtkMouseEventArgs> callback, float priority)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return this.AddObserver(
            eventId,
            (caller, actualEventId, _, _) =>
            {
                this.GetEventPosition(out var x, out var y);
                this.GetLastEventPosition(out var lastX, out var lastY);
                callback(new VtkMouseEventArgs(
                    this,
                    caller,
                    actualEventId,
                    x,
                    y,
                    lastX,
                    lastY,
                    this.GetControlKey(),
                    this.GetShiftKey(),
                    this.GetAltKey()));
            },
            priority: priority);
    }

    private VtkObserverHandle AddKeyObserver(uint eventId, Action<VtkKeyEventArgs> callback, float priority)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return this.AddObserver(
            eventId,
            (caller, actualEventId, _, _) =>
            {
                callback(new VtkKeyEventArgs(
                    this,
                    caller,
                    actualEventId,
                    this.GetKeyCode(),
                    this.GetKeySym(),
                    this.GetRepeatCount(),
                    this.GetControlKey(),
                    this.GetShiftKey(),
                    this.GetAltKey()));
            },
            priority: priority);
    }

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_GetEventPosition(nint self, int* position);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_GetLastEventPosition(nint self, int* position);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetControlKey(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetShiftKey(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetAltKey(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern byte vtkRenderWindowInteractor_GetKeyCode(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkRenderWindowInteractor_GetKeySym(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetRepeatCount(nint self);
}
