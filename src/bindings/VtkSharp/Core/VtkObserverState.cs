namespace VtkSharp;

internal sealed class VtkObserverState
{
    public VtkObserverState(vtkObject owner, VtkObjectEventHandler callback)
    {
        this.Owner = owner;
        this.Callback = callback;
    }

    public VtkObserverState(vtkObject owner, VtkObjectEventDataHandler dataCallback, object? clientData)
    {
        this.Owner = owner;
        this.DataCallback = dataCallback;
        this.ClientData = clientData;
    }

    public vtkObject Owner { get; }
    public VtkObjectEventHandler? Callback { get; }
    public VtkObjectEventDataHandler? DataCallback { get; }
    public object? ClientData { get; }
}
