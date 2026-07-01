using System;
using System.Windows.Threading;

namespace VtkSharp.Wpf;

public sealed partial class VtkOpenGlD3DImageRenderControl
{
    private void AttachTimerObservers(vtkRenderWindowInteractor interactor)
    {
        this._createTimerObserver = interactor.AddObserver(vtkCommand.CreateTimerEvent, (_, _) => this.CreatePlatformTimer());
        this._destroyTimerObserver = interactor.AddObserver(vtkCommand.DestroyTimerEvent, (_, _) => this.DestroyPlatformTimer());
    }

    private void DetachTimerObservers()
    {
        this._createTimerObserver?.Dispose();
        this._destroyTimerObserver?.Dispose();
        this._createTimerObserver = null;
        this._destroyTimerObserver = null;

        this.StopPlatformTimers();
    }

    private void StopPlatformTimers()
    {
        foreach (var timer in this._timers.Values)
        {
            timer.DispatcherTimer.Stop();
            timer.DispatcherTimer.Tick -= timer.OnTick;
        }

        this._timers.Clear();
    }

    private void CreatePlatformTimer()
    {
        if (this.Interactor is null) return;

        var timerId = this.Interactor.GetTimerEventId();
        var timerType = this.Interactor.GetTimerEventType();
        var duration = Math.Max(1, this.Interactor.GetTimerEventDuration());
        var platformTimerId = ++this._nextPlatformTimerId;

        EventHandler? onTick = null;
        var dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(duration)
        };

        onTick = (_, _) =>
        {
            if (this.Interactor is null) return;

            if (timerType == vtkRenderWindowInteractor.OneShotTimer)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Tick -= onTick;
                this._timers.Remove(platformTimerId);
            }

            this.Interactor.SetTimerEventId(timerId);
            if (this.Interactor is vtkGenericRenderWindowInteractor genericInteractor)
            {
                genericInteractor.TimerEvent();
            }
            else
            {
                this.Interactor.InvokeTimerEvent(timerId);
            }

            this.RequestRender();
        };

        dispatcherTimer.Tick += onTick;
        this._timers.Add(platformTimerId, new VtkDispatcherTimer(dispatcherTimer, onTick));
        this.Interactor.SetTimerEventPlatformId(platformTimerId);
        dispatcherTimer.Start();
    }

    private void DestroyPlatformTimer()
    {
        if (this.Interactor is null) return;

        var platformTimerId = this.Interactor.GetTimerEventPlatformId();
        if (!this._timers.TryGetValue(platformTimerId, out var timer)) return;

        this._timers.Remove(platformTimerId);
        timer.DispatcherTimer.Stop();
        timer.DispatcherTimer.Tick -= timer.OnTick;
    }

    private sealed record VtkDispatcherTimer(DispatcherTimer DispatcherTimer, EventHandler OnTick);
}
