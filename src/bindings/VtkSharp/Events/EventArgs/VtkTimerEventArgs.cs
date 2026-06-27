namespace VtkSharp;

public sealed class VtkTimerEventArgs : VtkEventArgs
{
    public VtkTimerEventArgs(vtkObject caller, uint eventId, int timerId) : base(caller, eventId)
    {
        this.TimerId = timerId;
    }

    public int TimerId { get; }
}