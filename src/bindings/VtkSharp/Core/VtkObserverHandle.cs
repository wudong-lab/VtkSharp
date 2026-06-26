using System;
using System.Runtime.InteropServices;

namespace VtkSharp;

public sealed class VtkObserverHandle : IDisposable
{
    private readonly vtkObject _owner;
    private GCHandle _stateHandle;
    private bool _disposed;

    internal VtkObserverHandle(vtkObject owner, ulong tag, GCHandle stateHandle)
    {
        this._owner = owner;
        this.Tag = tag;
        this._stateHandle = stateHandle;
    }

    public ulong Tag { get; }

    public void Dispose()
    {
        this.DisposeCore(removeFromOwner: true);
    }

    internal void DisposeFromOwner()
    {
        this.DisposeCore(removeFromOwner: false);
    }

    private void DisposeCore(bool removeFromOwner)
    {
        if (this._disposed) return;

        if (this._owner.NativePointer != IntPtr.Zero)
            this._owner.RemoveObserver(this.Tag);

        if (this._stateHandle.IsAllocated)
            this._stateHandle.Free();

        if (removeFromOwner)
            this._owner.DetachObserverHandle(this);

        this._disposed = true;
    }
}