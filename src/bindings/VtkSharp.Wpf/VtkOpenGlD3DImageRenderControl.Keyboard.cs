using System.Windows.Input;

namespace VtkSharp.Wpf;

public sealed partial class VtkOpenGlD3DImageRenderControl
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (this.Interactor is null) return;

        this.SetInteractorKeyEventInformation(e);
        this.InvokeInteractorEvent(vtkCommand.KeyPressEvent);
        if (IsCharEventKey(e))
        {
            this.InvokeInteractorEvent(vtkCommand.CharEvent);
        }

        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (this.Interactor is null) return;

        this.SetInteractorKeyEventInformation(e);
        this.InvokeInteractorEvent(vtkCommand.KeyReleaseEvent);
        this.RequestRender();
        e.Handled = true;
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);

        if (this.Interactor is null || string.IsNullOrEmpty(e.Text)) return;

        this.SetInteractorTextEventInformation(e.Text);
        this.InvokeInteractorEvent(vtkCommand.CharEvent);
        this.RequestRender();
        e.Handled = true;
    }

    private void SetInteractorKeyEventInformation(KeyEventArgs e)
    {
        if (this.Interactor is null) return;

        var modifiers = Keyboard.Modifiers;
        this.Interactor.SetKeyEventInformation(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            GetKeyCode(e),
            e.IsRepeat ? 1 : 0,
            GetKeySym(e));
        this.Interactor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private void SetInteractorTextEventInformation(string text)
    {
        if (this.Interactor is null || text.Length == 0) return;

        var modifiers = Keyboard.Modifiers;
        var keyCode = text[0] <= byte.MaxValue ? text[0] : '\0';
        var keySym = text.Length == 1 ? text : null;
        this.Interactor.SetKeyEventInformation(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Shift),
            keyCode,
            repeatCount: 0,
            keySym);
        this.Interactor.SetAltKey(modifiers.HasFlag(ModifierKeys.Alt));
    }

    private static char GetKeyCode(KeyEventArgs e)
    {
        var keySym = GetKeySym(e);
        if (keySym?.Length == 1 && keySym[0] <= byte.MaxValue)
        {
            return keySym[0];
        }

        return '\0';
    }

    private static bool IsCharEventKey(KeyEventArgs e)
    {
        var keySym = GetKeySym(e);
        return keySym?.Length == 1 || keySym is "BackSpace" or "Tab" or "Return" or "Escape" or "space";
    }

    private static string? GetKeySym(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        return key switch
        {
            Key.A => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "A" : "a",
            Key.B => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "B" : "b",
            Key.C => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "C" : "c",
            Key.D => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "D" : "d",
            Key.E => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "E" : "e",
            Key.F => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "F" : "f",
            Key.G => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "G" : "g",
            Key.H => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "H" : "h",
            Key.I => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "I" : "i",
            Key.J => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "J" : "j",
            Key.K => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "K" : "k",
            Key.L => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "L" : "l",
            Key.M => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "M" : "m",
            Key.N => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "N" : "n",
            Key.O => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "O" : "o",
            Key.P => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "P" : "p",
            Key.Q => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Q" : "q",
            Key.R => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "R" : "r",
            Key.S => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "S" : "s",
            Key.T => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "T" : "t",
            Key.U => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "U" : "u",
            Key.V => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "V" : "v",
            Key.W => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "W" : "w",
            Key.X => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "X" : "x",
            Key.Y => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Y" : "y",
            Key.Z => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? "Z" : "z",
            Key.D0 or Key.NumPad0 => "0",
            Key.D1 or Key.NumPad1 => "1",
            Key.D2 or Key.NumPad2 => "2",
            Key.D3 or Key.NumPad3 => "3",
            Key.D4 or Key.NumPad4 => "4",
            Key.D5 or Key.NumPad5 => "5",
            Key.D6 or Key.NumPad6 => "6",
            Key.D7 or Key.NumPad7 => "7",
            Key.D8 or Key.NumPad8 => "8",
            Key.D9 or Key.NumPad9 => "9",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "Prior",
            Key.PageDown => "Next",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Back => "BackSpace",
            Key.Tab => "Tab",
            Key.Enter or Key.Return => "Return",
            Key.Escape => "Escape",
            Key.Space => "space",
            Key.LeftShift or Key.RightShift => "Shift_L",
            Key.LeftCtrl or Key.RightCtrl => "Control_L",
            Key.LeftAlt or Key.RightAlt => "Alt_L",
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            Key.F6 => "F6",
            Key.F7 => "F7",
            Key.F8 => "F8",
            Key.F9 => "F9",
            Key.F10 => "F10",
            Key.F11 => "F11",
            Key.F12 => "F12",
            _ => null
        };
    }
}
