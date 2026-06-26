namespace VtkSharp;

internal sealed class VtkObserverState(vtkObject owner, VtkObjectEventHandler callback)
{
    public vtkObject Owner { get; } = owner;
    public VtkObjectEventHandler Callback { get; } = callback;
}