using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindowInteractor
{
    public const int OneShotTimer = 1;
    public const int RepeatingTimer = 2;

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

    public int CreateRepeatingTimer(int duration)
    {
        return vtkRenderWindowInteractor_CreateRepeatingTimer(this.NativePointer, duration);
    }

    public int CreateOneShotTimer(int duration)
    {
        return vtkRenderWindowInteractor_CreateOneShotTimer(this.NativePointer, duration);
    }

    public int DestroyTimer(int timerId)
    {
        return vtkRenderWindowInteractor_DestroyTimer_int(this.NativePointer, timerId);
    }

    public int GetTimerEventId()
    {
        return vtkRenderWindowInteractor_GetTimerEventId(this.NativePointer);
    }

    public void SetTimerEventId(int timerId)
    {
        vtkRenderWindowInteractor_SetTimerEventId(this.NativePointer, timerId);
    }

    public int GetTimerEventType()
    {
        return vtkRenderWindowInteractor_GetTimerEventType(this.NativePointer);
    }

    public int GetTimerEventDuration()
    {
        return vtkRenderWindowInteractor_GetTimerEventDuration(this.NativePointer);
    }

    public int GetTimerEventPlatformId()
    {
        return vtkRenderWindowInteractor_GetTimerEventPlatformId(this.NativePointer);
    }

    public void SetTimerEventPlatformId(int platformTimerId)
    {
        vtkRenderWindowInteractor_SetTimerEventPlatformId(this.NativePointer, platformTimerId);
    }

    public void InvokeTimerEvent(int timerId)
    {
        vtkRenderWindowInteractor_InvokeTimerEvent(this.NativePointer, timerId);
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

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_CreateRepeatingTimer(nint self, int duration);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_CreateOneShotTimer(nint self, int duration);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_DestroyTimer_int(nint self, int timerId);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetTimerEventId(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetTimerEventId(nint self, int timerId);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetTimerEventType(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetTimerEventDuration(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindowInteractor_GetTimerEventPlatformId(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_SetTimerEventPlatformId(nint self, int platformTimerId);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_InvokeTimerEvent(nint self, int timerId);
    #endregion
}
