namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkObjectObserverTests
{
    [TestMethod]
    public void AddObserver_InvokesManagedCallbackWhenEventFires()
    {
        using var points = vtkPoints.New();
        var callbackCount = 0;
        vtkObject? observedCaller = null;
        uint observedEventId = 0;

        using var observer = points.AddObserver(vtkCommand.ModifiedEvent, (caller, eventId) =>
        {
            callbackCount++;
            observedCaller = caller;
            observedEventId = eventId;
        });

        points.Modified();

        Assert.AreEqual(1, callbackCount);
        Assert.AreSame(points, observedCaller);
        Assert.AreEqual(vtkCommand.ModifiedEvent, observedEventId);
        Assert.IsTrue(observer.Tag > 0);
    }

    [TestMethod]
    public unsafe void AddObserver_PassesClientDataAndCallDataToManagedCallback()
    {
        using var points = vtkPoints.New();
        var expectedClientData = new ObserverClientData("test-client");
        var callDataValue = 42;
        object? observedClientData = null;
        nint observedCallData = 0;

        using var observer = points.AddObserver(
            vtkCommand.UserEvent,
            (caller, eventId, clientData, callData) =>
            {
                observedClientData = clientData;
                observedCallData = callData;
            },
            expectedClientData);

        points.InvokeEvent(vtkCommand.UserEvent, (nint)(&callDataValue));

        Assert.AreSame(expectedClientData, observedClientData);
        Assert.AreEqual((nint)(&callDataValue), observedCallData);
        Assert.AreEqual(42, *(int*)observedCallData);
    }

    [TestMethod]
    public void ObserverDispose_RemovesObserver()
    {
        using var points = vtkPoints.New();
        var callbackCount = 0;

        var observer = points.AddObserver(vtkCommand.ModifiedEvent, (_, _) => callbackCount++);
        observer.Dispose();

        points.Modified();

        Assert.AreEqual(0, callbackCount);
    }

    [TestMethod]
    public void OwnerDispose_ReleasesObserverHandle()
    {
        var points = vtkPoints.New();
        var observer = points.AddObserver(vtkCommand.ModifiedEvent, (_, _) => { });

        points.Dispose();
        observer.Dispose();

        Assert.AreEqual(0, points.NativePointer);
    }

    private sealed record ObserverClientData(string Name);
}
