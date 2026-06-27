namespace VtkSharp;

public sealed class VtkProgressEventArgs : VtkEventArgs
{
    public VtkProgressEventArgs(vtkObject caller, uint eventId, double progress) : base(caller, eventId)
    {
        this.Progress = progress;
    }

    public double Progress { get; }
}