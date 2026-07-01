using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindowInteractor
{
    public const int OneShotTimer = 1;
    public const int RepeatingTimer = 2;

    public void SetAltKey(bool altKey)
    {
        this.SetAltKey(altKey ? 1 : 0);
    }

    public void SetEventInformationFlipY(
        int x,
        int y,
        bool controlKey,
        bool shiftKey,
        char keyCode = '\0',
        int repeatCount = 0,
        string? keySym = null)
    {
        this.SetEventInformationFlipY(
            x,
            y,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            keyCode,
            repeatCount,
            keySym ?? string.Empty);
    }

    public void SetEventInformation(
        int x,
        int y,
        bool controlKey,
        bool shiftKey,
        char keyCode = '\0',
        int repeatCount = 0,
        string? keySym = null)
    {
        this.SetEventInformation(
            x,
            y,
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            keyCode,
            repeatCount,
            keySym ?? string.Empty);
    }

    public void SetKeyEventInformation(
        bool controlKey,
        bool shiftKey,
        char keyCode,
        int repeatCount = 0,
        string? keySym = null)
    {
        this.SetKeyEventInformation(
            controlKey ? 1 : 0,
            shiftKey ? 1 : 0,
            keyCode,
            repeatCount,
            keySym ?? string.Empty);
    }

    public int CreateRepeatingTimer(int duration)
    {
        return this.CreateRepeatingTimer((ulong)duration);
    }

    public int CreateOneShotTimer(int duration)
    {
        return this.CreateOneShotTimer((ulong)duration);
    }

    public void InvokeTimerEvent(int timerId)
    {
        vtkRenderWindowInteractor_InvokeTimerEvent(this.NativePointer, timerId);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindowInteractor_InvokeTimerEvent(nint self, int timerId);
    #endregion
}
