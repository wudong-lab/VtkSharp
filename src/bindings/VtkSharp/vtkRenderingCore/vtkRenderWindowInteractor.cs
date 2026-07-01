using System;
using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindowInteractor
{
    public void GetEventPosition(out int x, out int y)
    {
        var position = this.GetEventPosition_Internal();
        x = position[0];
        y = position[1];
    }

    public void GetLastEventPosition(out int x, out int y)
    {
        var position = this.GetLastEventPosition_Internal();
        x = position[0];
        y = position[1];
    }

    public VtkObserverHandle AddMouseMoveEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.MouseMoveEvent, callback, priority);
    }

    public VtkObserverHandle AddLeftButtonPressEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.LeftButtonPressEvent, callback, priority);
    }

    public VtkObserverHandle AddLeftButtonReleaseEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.LeftButtonReleaseEvent, callback, priority);
    }

    public VtkObserverHandle AddRightButtonPressEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.RightButtonPressEvent, callback, priority);
    }

    public VtkObserverHandle AddRightButtonReleaseEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.RightButtonReleaseEvent, callback, priority);
    }

    public VtkObserverHandle AddMouseWheelForwardEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.MouseWheelForwardEvent, callback, priority);
    }

    public VtkObserverHandle AddMouseWheelBackwardEventObserver(Action<VtkMouseEventArgs> callback, float priority = 0.0f)
    {
        return this.AddMouseObserver(vtkCommand.MouseWheelBackwardEvent, callback, priority);
    }

    public VtkObserverHandle AddKeyPressEventObserver(Action<VtkKeyEventArgs> callback, float priority = 0.0f)
    {
        return this.AddKeyObserver(vtkCommand.KeyPressEvent, callback, priority);
    }

    public VtkObserverHandle AddKeyReleaseEventObserver(Action<VtkKeyEventArgs> callback, float priority = 0.0f)
    {
        return this.AddKeyObserver(vtkCommand.KeyReleaseEvent, callback, priority);
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
                    this.IsControlKeyPressed(),
                    this.IsShiftKeyPressed(),
                    this.IsAltKeyPressed()));
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
                    this.IsControlKeyPressed(),
                    this.IsShiftKeyPressed(),
                    this.IsAltKeyPressed()));
            },
            priority: priority);
    }

    private bool IsControlKeyPressed() => this.GetControlKey() != 0;
    private bool IsShiftKeyPressed() => this.GetShiftKey() != 0;
    private bool IsAltKeyPressed() => this.GetAltKey() != 0;

    public const int OneShotTimer = 1;
    public const int RepeatingTimer = 2;

    public void SetAltKey(bool altKey)
    {
        this.SetAltKey(altKey ? 1 : 0);
    }

    public void SetEventInformationFlipY(
        int x,
        int y,
        bool controlKey,
        bool shiftKey,
        char keyCode = '\0',
        int repeatCount = 0,
        string? keySym = null)
    {
        this.SetEventInformationFlipY(
            x,
            y,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            keyCode,
            repeatCount,
            keySym ?? string.Empty);
    }

    public void SetKeyEventInformation(
        bool controlKey,
        bool shiftKey,
        char keyCode,
        int repeatCount = 0,
        string? keySym = null)
    {
        this.SetKeyEventInformation(
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            keyCode,
            repeatCount,
            keySym ?? string.Empty);
    }

    public void InvokeTimerEvent(int timerId)
    {
        vtkRenderWindowInteractor_InvokeTimerEvent(this.NativePointer, timerId);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_InvokeTimerEvent(nint self, int timerId);
    #endregion
}