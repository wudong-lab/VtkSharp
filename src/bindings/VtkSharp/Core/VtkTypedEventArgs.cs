namespace VtkSharp;

public class VtkEventArgs
{
    public VtkEventArgs(vtkObject caller, uint eventId)
    {
        this.Caller = caller;
        this.EventId = eventId;
    }

    public vtkObject Caller { get; }
    public uint EventId { get; }
}

public sealed class VtkProgressEventArgs : VtkEventArgs
{
    public VtkProgressEventArgs(vtkObject caller, uint eventId, double progress) : base(caller, eventId)
    {
        this.Progress = progress;
    }

    public double Progress { get; }
}

public sealed class VtkMessageEventArgs : VtkEventArgs
{
    public VtkMessageEventArgs(vtkObject caller, uint eventId, string message) : base(caller, eventId)
    {
        this.Message = message;
    }

    public string Message { get; }
}

public sealed class VtkTimerEventArgs : VtkEventArgs
{
    public VtkTimerEventArgs(vtkObject caller, uint eventId, int timerId) : base(caller, eventId)
    {
        this.TimerId = timerId;
    }

    public int TimerId { get; }
}

public sealed class VtkMouseEventArgs : VtkEventArgs
{
    public VtkMouseEventArgs(
        vtkRenderWindowInteractor interactor,
        vtkObject caller,
        uint eventId,
        int x,
        int y,
        int lastX,
        int lastY,
        bool controlKey,
        bool shiftKey,
        bool altKey)
        : base(caller, eventId)
    {
        this.Interactor = interactor;
        this.X = x;
        this.Y = y;
        this.LastX = lastX;
        this.LastY = lastY;
        this.ControlKey = controlKey;
        this.ShiftKey = shiftKey;
        this.AltKey = altKey;
    }

    public vtkRenderWindowInteractor Interactor { get; }
    public int X { get; }
    public int Y { get; }
    public int LastX { get; }
    public int LastY { get; }
    public bool ControlKey { get; }
    public bool ShiftKey { get; }
    public bool AltKey { get; }
}

public sealed class VtkKeyEventArgs : VtkEventArgs
{
    public VtkKeyEventArgs(
        vtkRenderWindowInteractor interactor,
        vtkObject caller,
        uint eventId,
        char keyCode,
        string keySym,
        int repeatCount,
        bool controlKey,
        bool shiftKey,
        bool altKey)
        : base(caller, eventId)
    {
        this.Interactor = interactor;
        this.KeyCode = keyCode;
        this.KeySym = keySym;
        this.RepeatCount = repeatCount;
        this.ControlKey = controlKey;
        this.ShiftKey = shiftKey;
        this.AltKey = altKey;
    }

    public vtkRenderWindowInteractor Interactor { get; }
    public char KeyCode { get; }
    public string KeySym { get; }
    public int RepeatCount { get; }
    public bool ControlKey { get; }
    public bool ShiftKey { get; }
    public bool AltKey { get; }
}
