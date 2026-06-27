namespace VtkSharp;

public sealed class VtkMessageEventArgs : VtkEventArgs
{
    public VtkMessageEventArgs(vtkObject caller, uint eventId, string message) : base(caller, eventId)
    {
        this.Message = message;
    }

    public string Message { get; }
}