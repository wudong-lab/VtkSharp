using System;

namespace VtkSharp;

public static class VtkObjectObserverExtensions
{
    public static VtkObserverHandle AddModifiedEventObserver(
        this vtkObject source,
        Action<VtkEventArgs> callback,
        float priority = 0.0f)
    {
        return AddEventObserver(source, VtkCommandEventIds.ModifiedEvent, callback, priority);
    }

    public static VtkObserverHandle AddStartEventObserver(
        this vtkObject source,
        Action<VtkEventArgs> callback,
        float priority = 0.0f)
    {
        return AddEventObserver(source, VtkCommandEventIds.StartEvent, callback, priority);
    }

    public static VtkObserverHandle AddEndEventObserver(
        this vtkObject source,
        Action<VtkEventArgs> callback,
        float priority = 0.0f)
    {
        return AddEventObserver(source, VtkCommandEventIds.EndEvent, callback, priority);
    }

    public static unsafe VtkObserverHandle AddProgressEventObserver(
        this vtkObject source,
        Action<VtkProgressEventArgs> callback,
        float priority = 0.0f)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return source.AddObserver(
            VtkCommandEventIds.ProgressEvent,
            (caller, eventId, _, callData) =>
            {
                var progress = callData == 0 ? 0.0 : *(double*)callData;
                callback(new VtkProgressEventArgs(caller, eventId, progress));
            },
            priority: priority);
    }

    public static VtkObserverHandle AddErrorEventObserver(
        this vtkObject source,
        Action<VtkMessageEventArgs> callback,
        float priority = 0.0f)
    {
        return AddMessageObserver(source, VtkCommandEventIds.ErrorEvent, callback, priority);
    }

    public static VtkObserverHandle AddWarningEventObserver(
        this vtkObject source,
        Action<VtkMessageEventArgs> callback,
        float priority = 0.0f)
    {
        return AddMessageObserver(source, VtkCommandEventIds.WarningEvent, callback, priority);
    }

    public static unsafe VtkObserverHandle AddTimerEventObserver(
        this vtkObject source,
        Action<VtkTimerEventArgs> callback,
        float priority = 0.0f)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return source.AddObserver(
            VtkCommandEventIds.TimerEvent,
            (caller, eventId, _, callData) =>
            {
                var timerId = callData == 0 ? 0 : *(int*)callData;
                callback(new VtkTimerEventArgs(caller, eventId, timerId));
            },
            priority: priority);
    }

    private static VtkObserverHandle AddEventObserver(
        vtkObject source,
        uint eventId,
        Action<VtkEventArgs> callback,
        float priority)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return source.AddObserver(
            eventId,
            (caller, actualEventId, _, _) => callback(new VtkEventArgs(caller, actualEventId)),
            priority: priority);
    }

    private static VtkObserverHandle AddMessageObserver(
        vtkObject source,
        uint eventId,
        Action<VtkMessageEventArgs> callback,
        float priority)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        return source.AddObserver(
            eventId,
            (caller, actualEventId, _, callData) =>
            {
                var message = VtkString.FromUtf8Pointer(callData);
                callback(new VtkMessageEventArgs(caller, actualEventId, message));
            },
            priority: priority);
    }
}
