using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkRenderWindow
{
    public const int CursorDefault = 0;
    public const int CursorArrow = 1;
    public const int CursorSizeNE = 2;
    public const int CursorSizeNW = 3;
    public const int CursorSizeSW = 4;
    public const int CursorSizeSE = 5;
    public const int CursorSizeNS = 6;
    public const int CursorSizeWE = 7;
    public const int CursorSizeAll = 8;
    public const int CursorHand = 9;
    public const int CursorCrosshair = 10;
    public const int CursorCustom = 11;

    public int GetCurrentCursor()
    {
        return vtkRenderWindow_GetCurrentCursor(this.NativePointer);
    }

    public void SetCurrentCursor(int cursor)
    {
        vtkRenderWindow_SetCurrentCursor(this.NativePointer, cursor);
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern int vtkRenderWindow_GetCurrentCursor(nint self);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void vtkRenderWindow_SetCurrentCursor(nint self, int cursor);
    #endregion
}
