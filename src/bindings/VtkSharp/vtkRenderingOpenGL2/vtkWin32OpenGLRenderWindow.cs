namespace VtkSharp;

public unsafe partial class vtkWin32OpenGLRenderWindow
{
    /// <summary>Returns a borrowed pointer to the window width and height.</summary>
    /// <remarks>The returned two-element buffer is owned by this render window and must not be freed.</remarks>
    public int* GetSize() => this.GetSize_Internal();
}
