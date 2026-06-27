namespace VtkSharp;

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