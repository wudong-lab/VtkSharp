using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace VtkSharp.Wpf;

public sealed partial class VtkRenderControl
{
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);

        if (this.RenderWindowInteractor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(vtkCommand.EnterEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (this.RenderWindowInteractor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(vtkCommand.LeaveEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
        {
            this.Focus();
            this.FitIntoView();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
        {
            this.Focus();
            this.CaptureMouse();
            this._activeMouseButton = e.ChangedButton;
            this.InvokeMouseButtonEvent(e.ChangedButton, pressed: true, e.GetPosition(this), e.ClickCount);
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (this.RenderWindowInteractor is null)
        {
            return;
        }

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(vtkCommand.MouseMoveEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == this._activeMouseButton)
        {
            this.InvokeMouseButtonEvent(e.ChangedButton, pressed: false, e.GetPosition(this), repeatCount: 0);
            this._activeMouseButton = null;
            this.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        if (this._activeMouseButton is not { } activeButton)
        {
            return;
        }

        this.InvokeMouseButtonEvent(activeButton, pressed: false, e.GetPosition(this), repeatCount: 0);
        this._activeMouseButton = null;
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (this.RenderWindowInteractor is null) return;

        this.SetInteractorEventInformation(e.GetPosition(this), repeatCount: 0);
        this.InvokeInteractorEvent(e.Delta > 0
            ? vtkCommand.MouseWheelForwardEvent
            : vtkCommand.MouseWheelBackwardEvent);
        this.RequestRender();
        e.Handled = true;
    }

    private void InvokeMouseButtonEvent(MouseButton button, bool pressed, Point position, int repeatCount)
    {
        if (this.RenderWindowInteractor is null) return;

        this.SetInteractorEventInformation(position, repeatCount);
        this.InvokeInteractorEvent(GetMouseButtonEvent(button, pressed));
        this.RequestRender();
    }

    private void InvokeInteractorEvent(uint eventId)
    {
        if (this.RenderWindowInteractor is null) return;

        if (this.RenderWindowInteractor is vtkGenericRenderWindowInteractor genericInteractor)
        {
            InvokeGenericInteractorEvent(genericInteractor, eventId);
            this.SyncCursor();
            return;
        }

        this.RenderWindowInteractor.InvokeEvent(eventId);
        this.SyncCursor();
    }

    private static void InvokeGenericInteractorEvent(vtkGenericRenderWindowInteractor interactor, uint eventId)
    {
        switch (eventId)
        {
            case vtkCommand.MouseMoveEvent:
                interactor.MouseMoveEvent();
                break;
            case vtkCommand.LeftButtonPressEvent:
                interactor.LeftButtonPressEvent();
                break;
            case vtkCommand.LeftButtonReleaseEvent:
                interactor.LeftButtonReleaseEvent();
                break;
            case vtkCommand.MiddleButtonPressEvent:
                interactor.MiddleButtonPressEvent();
                break;
            case vtkCommand.MiddleButtonReleaseEvent:
                interactor.MiddleButtonReleaseEvent();
                break;
            case vtkCommand.RightButtonPressEvent:
                interactor.RightButtonPressEvent();
                break;
            case vtkCommand.RightButtonReleaseEvent:
                interactor.RightButtonReleaseEvent();
                break;
            case vtkCommand.MouseWheelForwardEvent:
                interactor.MouseWheelForwardEvent();
                break;
            case vtkCommand.MouseWheelBackwardEvent:
                interactor.MouseWheelBackwardEvent();
                break;
            case vtkCommand.EnterEvent:
                interactor.EnterEvent();
                break;
            case vtkCommand.LeaveEvent:
                interactor.LeaveEvent();
                break;
            case vtkCommand.KeyPressEvent:
                interactor.KeyPressEvent();
                break;
            case vtkCommand.KeyReleaseEvent:
                interactor.KeyReleaseEvent();
                break;
            case vtkCommand.CharEvent:
                interactor.CharEvent();
                break;
            default:
                interactor.InvokeEvent(eventId);
                break;
        }
    }

    private void SetInteractorEventInformation(Point position, int repeatCount)
    {
        if (this.RenderWindowInteractor is null) return;

        var pixelPosition = this.GetPixelPosition(position);
        var modifiers = Keyboard.Modifiers;
        this.RenderWindowInteractor.SetEventInformationFlipY(
            pixelPosition.X,
            pixelPosition.Y,
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            keyCode: '\0',
            repeatCount);
        this.RenderWindowInteractor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private PixelPoint GetPixelPosition(Point position)
    {
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));

        var x = ClampPixelCoordinate((int)Math.Round(position.X * transform.M11), pixelSize.Width);
        var y = ClampPixelCoordinate((int)Math.Round(position.Y * transform.M22), pixelSize.Height);
        return new PixelPoint(x, y);
    }

    private static int ClampPixelCoordinate(int value, int length)
    {
        return Math.Min(Math.Max(value, 0), length - 1);
    }

    private static uint GetMouseButtonEvent(MouseButton button, bool pressed)
    {
        return button switch
        {
            MouseButton.Left => pressed ? vtkCommand.LeftButtonPressEvent : vtkCommand.LeftButtonReleaseEvent,
            MouseButton.Middle => pressed ? vtkCommand.MiddleButtonPressEvent : vtkCommand.MiddleButtonReleaseEvent,
            MouseButton.Right => pressed ? vtkCommand.RightButtonPressEvent : vtkCommand.RightButtonReleaseEvent,
            _ => vtkCommand.NoEvent,
        };
    }

    private readonly record struct PixelPoint(int X, int Y);
}
