using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VtkSharp;

public class vtkObjectBase : IDisposable
{
    protected vtkObjectBase(nint nativePointer, bool ownsReference)
    {
        Debug.Assert(nativePointer != IntPtr.Zero);
        this.NativePointer = nativePointer;
        this.OwnsReference = ownsReference;
    }

    public nint NativePointer { get; protected set; }
    public bool OwnsReference { get; }

    public virtual void Delete()
    {
        if (!this.OwnsReference || this.NativePointer == IntPtr.Zero) return;

        vtkObjectBase_Delete(this.NativePointer);
        this.NativePointer = IntPtr.Zero;
    }

    public void Register() => vtkObjectBase_Register(this.NativePointer);
    public void UnRegister() => vtkObjectBase_UnRegister(this.NativePointer);

    public int GetReferenceCount() => this.NativePointer == IntPtr.Zero ? 0 : vtkObjectBase_GetReferenceCount(this.NativePointer);
    public int ReferenceCount => this.GetReferenceCount();

    public void Dispose()
    {
        this.Delete();
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObjectBase_Delete(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObjectBase_Register(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkObjectBase_UnRegister(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkObjectBase_GetReferenceCount(nint self);
    #endregion
}
