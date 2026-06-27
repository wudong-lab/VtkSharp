namespace VtkSharp;

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