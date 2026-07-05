using System.Windows.Input;

namespace VtkSharp.Wpf;

public sealed partial class VtkRenderControl
{
    private void AttachCursorObserver(vtkRenderWindow renderWindow)
    {
        this._cursorChangedObserver = renderWindow.AddObserver(vtkCommand.CursorChangedEvent, (_, _) => this.SyncCursor());
        this.SyncCursor();
    }

    private void DetachCursorObserver()
    {
        this._cursorChangedObserver?.Dispose();
        this._cursorChangedObserver = null;
    }

    private void SyncCursor()
    {
        if (this.RenderWindow is null) return;

        var cursor = MapVtkCursor(this.RenderWindow.GetCurrentCursor());
        if (this.Cursor != cursor)
        {
            this.Cursor = cursor;
        }
    }

    private static Cursor MapVtkCursor(int vtkCursor)
    {
        return vtkCursor switch
        {
            vtkRenderWindow.CursorSizeNE or vtkRenderWindow.CursorSizeSW => Cursors.SizeNESW,
            vtkRenderWindow.CursorSizeNW or vtkRenderWindow.CursorSizeSE => Cursors.SizeNWSE,
            vtkRenderWindow.CursorSizeNS => Cursors.SizeNS,
            vtkRenderWindow.CursorSizeWE => Cursors.SizeWE,
            vtkRenderWindow.CursorSizeAll => Cursors.SizeAll,
            vtkRenderWindow.CursorHand => Cursors.Hand,
            vtkRenderWindow.CursorCrosshair => Cursors.Cross,
            vtkRenderWindow.CursorDefault or vtkRenderWindow.CursorArrow or vtkRenderWindow.CursorCustom => Cursors.Arrow,
            _ => Cursors.Arrow,
        };
    }
}
