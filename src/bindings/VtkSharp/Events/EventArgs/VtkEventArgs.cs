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