using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkWindow : vtkObject
{
    protected vtkWindow(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) { }
    public new static vtkWindow WeakReference(nint nativePointer) => new(nativePointer, ownsReference: false);

    public new static vtkWindow Register(vtkWindow sourceObject)
    {
        var target = new vtkWindow(sourceObject.NativePointer, true);
        target.Register();
        return target;
    }

    public new void SetSize(int width, int height)
    {
        vtkWindow_SetSize(this.NativePointer, width, height);
    }

    public new void ShowWindowOn()
    {
        vtkWindow_ShowWindowOn(this.NativePointer);
    }

    public new void ShowWindowOff()
    {
        vtkWindow_ShowWindowOff(this.NativePointer);
    }

    public new void SetWindowName(string _arg)
    {
        vtkWindow_SetWindowName(this.NativePointer, _arg);
    }

    public new void Render()
    {
        vtkWindow_Render(this.NativePointer);
    }

    public new void OffScreenRenderingOn()
    {
        vtkWindow_OffScreenRenderingOn(this.NativePointer);
    }

    public new void OffScreenRenderingOff()
    {
        vtkWindow_OffScreenRenderingOff(this.NativePointer);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_SetSize(nint self, int width, int height);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_ShowWindowOn(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_ShowWindowOff(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint vtkWindow_GetWindowName(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_SetWindowName(nint self, string _arg);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_Render(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_OffScreenRenderingOn(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkWindow_OffScreenRenderingOff(nint self);
    #endregion
}