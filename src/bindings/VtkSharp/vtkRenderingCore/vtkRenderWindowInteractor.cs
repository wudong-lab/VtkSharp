using System;

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
}
