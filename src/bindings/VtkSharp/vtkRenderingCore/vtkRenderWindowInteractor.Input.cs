using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindowInteractor
{
    public void EnableRenderOff()
    {
        vtkRenderWindowInteractor_EnableRenderOff(this.NativePointer);
    }

    public void EnableRenderOn()
    {
        vtkRenderWindowInteractor_EnableRenderOn(this.NativePointer);
    }

    public void SetAltKey(bool altKey)
    {
        vtkRenderWindowInteractor_SetAltKey(this.NativePointer, altKey ? 1 : 0);
    }

    public void SetEventInformationFlipY(
        int x,
        int y,
        bool controlKey,
        bool shiftKey,
        char keyCode = '\0',
        int repeatCount = 0)
    {
        vtkRenderWindowInteractor_SetEventInformationFlipY(
            this.NativePointer,
            x,
            y,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            (byte)keyCode,
            repeatCount);
    }

    public void SetEventInformation(
        int x,
        int y,
        bool controlKey,
        bool shiftKey,
        char keyCode = '\0',
        int repeatCount = 0)
    {
        vtkRenderWindowInteractor_SetEventInformation(
            this.NativePointer,
            x,
            y,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            (byte)keyCode,
            repeatCount);
    }

    public void SetKeyEventInformation(
        bool controlKey,
        bool shiftKey,
        char keyCode,
        int repeatCount = 0)
    {
        vtkRenderWindowInteractor_SetKeyEventInformation(
            this.NativePointer,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            (byte)keyCode,
            repeatCount);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_EnableRenderOff(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_EnableRenderOn(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetAltKey(nint self, int altKey);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetEventInformationFlipY(
        nint self,
        int x,
        int y,
        int controlKey,
        int shiftKey,
        byte keyCode,
        int repeatCount);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetEventInformation(
        nint self,
        int x,
        int y,
        int controlKey,
        int shiftKey,
        byte keyCode,
        int repeatCount);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetKeyEventInformation(
        nint self,
        int controlKey,
        int shiftKey,
        byte keyCode,
        int repeatCount);
    #endregion
}
