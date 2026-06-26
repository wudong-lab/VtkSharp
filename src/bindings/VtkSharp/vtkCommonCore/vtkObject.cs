using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VtkSharp;

public class vtkObject : vtkObjectBase
{
    private static readonly VtkObserverNativeCallback ObserverCallback = OnObserverCallback;
    private static readonly nint ObserverCallbackPointer = Marshal.GetFunctionPointerForDelegate(ObserverCallback);

    private List<VtkObserverHandle>? _observerHandles;

    protected vtkObject(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) { }

    public override void Delete()
    {
        this.DisposeObserverHandles();
        base.Delete();
    }

    public void Modified()
    {
        vtkObject_Modified(this.NativePointer);
    }

    public VtkObserverHandle AddObserver(uint eventId, VtkObjectEventHandler callback, float priority = 0.0f)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        var state = new VtkObserverState(this, callback);
        var stateHandle = GCHandle.Alloc(state);

        try
        {
            var tag = vtkObject_AddObserverCallback(
                this.NativePointer,
                eventId,
                ObserverCallbackPointer,
                GCHandle.ToIntPtr(stateHandle),
                priority);

            var observer = new VtkObserverHandle(this, tag, stateHandle);
            this.AddObserverHandle(observer);
            return observer;
        }
        catch
        {
            if (stateHandle.IsAllocated)
            {
                stateHandle.Free();
            }

            throw;
        }
    }

    public void RemoveObserver(ulong tag)
    {
        vtkObject_RemoveObserver(this.NativePointer, tag);
    }

    internal void DetachObserverHandle(VtkObserverHandle observer)
    {
        this._observerHandles?.Remove(observer);
    }

    private void AddObserverHandle(VtkObserverHandle observer)
    {
        this._observerHandles ??= [];
        this._observerHandles.Add(observer);
    }

    private void DisposeObserverHandles()
    {
        if (this._observerHandles is null || this._observerHandles.Count == 0) return;

        var handles = this._observerHandles.ToArray();
        this._observerHandles.Clear();
        foreach (var handle in handles)
        {
            handle.DisposeFromOwner();
        }
    }

    private static void OnObserverCallback(nint caller, uint eventId, nint clientData)
    {
        var stateHandle = GCHandle.FromIntPtr(clientData);
        if (stateHandle.Target is VtkObserverState state)
        {
            state.Callback(state.Owner, eventId);
        }
    }

    #region Interop
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VtkObserverNativeCallback(nint caller, uint eventId, nint clientData);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObject_Modified(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern ulong vtkObject_AddObserverCallback(nint self, uint eventId, nint callback, nint clientData, float priority);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObject_RemoveObserver(nint self, ulong tag);
    #endregion
}