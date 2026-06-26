namespace VtkSharp.Tests;

public sealed class VtkObjectObserverTests
{
    [Fact]
    public void AddObserver_InvokesManagedCallbackWhenEventFires()
    {
        using var points = vtkPoints.New();
        var callbackCount = 0;
        vtkObject? observedCaller = null;
        uint observedEventId = 0;

        using var observer = points.AddObserver(VtkCommandEventIds.ModifiedEvent, (caller, eventId) =>
        {
            callbackCount++;
            observedCaller = caller;
            observedEventId = eventId;
        });

        points.Modified();

        Assert.Equal(1, callbackCount);
        Assert.Same(points, observedCaller);
        Assert.Equal(VtkCommandEventIds.ModifiedEvent, observedEventId);
        Assert.True(observer.Tag > 0);
    }

    [Fact]
    public void ObserverDispose_RemovesObserver()
    {
        using var points = vtkPoints.New();
        var callbackCount = 0;

        var observer = points.AddObserver(VtkCommandEventIds.ModifiedEvent, (_, _) => callbackCount++);
        observer.Dispose();

        points.Modified();

        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void OwnerDispose_ReleasesObserverHandle()
    {
        var points = vtkPoints.New();
        var observer = points.AddObserver(VtkCommandEventIds.ModifiedEvent, (_, _) => { });

        points.Dispose();
        observer.Dispose();

        Assert.Equal(0, points.NativePointer);
    }
}
