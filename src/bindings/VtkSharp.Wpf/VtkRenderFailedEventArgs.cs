namespace VtkSharp.Wpf;

public sealed class VtkRenderFailedEventArgs : EventArgs
{
    public VtkRenderFailedEventArgs(string message)
    {
        this.Message = message;
    }

    public string Message { get; }
}
