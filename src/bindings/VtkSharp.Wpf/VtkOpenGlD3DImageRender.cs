using System.Runtime.InteropServices;

namespace VtkSharp.Wpf;

internal sealed class VtkOpenGlD3DImageRender : IDisposable
{
    private nint _nativePointer;

    private VtkOpenGlD3DImageRender(nint nativePointer)
    {
        this._nativePointer = nativePointer;
    }

    public vtkRenderWindow RenderWindow => vtkRenderWindow.WeakReference(VtkOpenGlD3DImageRender_GetRenderWindow(this._nativePointer));

    public vtkRenderer Renderer => vtkRenderer.WeakReference(VtkOpenGlD3DImageRender_GetRenderer(this._nativePointer));

    public static VtkOpenGlD3DImageRender Create()
    {
        var nativePointer = VtkOpenGlD3DImageRender_New();
        if (nativePointer != IntPtr.Zero)
            return new VtkOpenGlD3DImageRender(nativePointer);

        var nativeError = Marshal.PtrToStringAnsi(VtkOpenGlD3DImageRender_GetLastError());
        var detail = string.IsNullOrWhiteSpace(nativeError)
            ? "The current GPU/driver may not support WGL_NV_DX_interop."
            : nativeError;

        throw new InvalidOperationException($"Failed to create internal VTK OpenGL/D3DImage render. {detail}");
    }

    public bool SetSize(int width, int height)
    {
        if (this._nativePointer == IntPtr.Zero) return false;

        return VtkOpenGlD3DImageRender_SetSize(this._nativePointer, width, height);
    }

    public nint GetBackBuffer()
    {
        return this._nativePointer == IntPtr.Zero
            ? IntPtr.Zero
            : VtkOpenGlD3DImageRender_GetBackBuffer(this._nativePointer);
    }

    public bool Render()
    {
        if (this._nativePointer == IntPtr.Zero) return false;

        return VtkOpenGlD3DImageRender_Render(this._nativePointer);
    }

    public string? GetLastError()
    {
        var nativeError = Marshal.PtrToStringAnsi(VtkOpenGlD3DImageRender_GetLastError());
        return string.IsNullOrWhiteSpace(nativeError) ? null : nativeError;
    }

    public void Dispose()
    {
        if (this._nativePointer == IntPtr.Zero) return;

        VtkOpenGlD3DImageRender_Delete(this._nativePointer);
        this._nativePointer = IntPtr.Zero;
    }

    #region Interop
    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_New();

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern void VtkOpenGlD3DImageRender_Delete(nint render);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetRenderWindow(nint render);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetRenderer(nint render);

    [DllImport(InteropInfo.NativeLibraryName)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool VtkOpenGlD3DImageRender_SetSize(nint render, int width, int height);

    [DllImport(InteropInfo.NativeLibraryName)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool VtkOpenGlD3DImageRender_Render(nint render);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetBackBuffer(nint render);

    [DllImport(InteropInfo.NativeLibraryName)]
    private static extern nint VtkOpenGlD3DImageRender_GetLastError();
    #endregion
}
