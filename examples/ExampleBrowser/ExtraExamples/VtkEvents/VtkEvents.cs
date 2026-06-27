using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("VTK Events", "ExtraExamples",
    Description = "Registers typed VTK event observers and renders a cone.",
    SourceFiles = new[] { "ExtraExamples/VtkEvents/VtkEvents.cs" })]
internal class VtkEvents : IExample
{
    private int _modifiedCount;
    private int _mouseMoveCount;

    public void Run()
    {
        using var cone = vtkConeSource.New();

        using var modifiedObserver = cone.AddModifiedEventObserver(this.ModifiedEventHandler);
        using var progressObserver = cone.AddProgressEventObserver(ProgressEventHandler);

        cone.SetHeight(3.0);
        cone.SetRadius(1.0);
        cone.SetResolution(32);
        cone.Update();

        Debug.WriteLine($"Modified observer tag: {modifiedObserver.Tag}");
        Debug.WriteLine($"Progress observer tag: {progressObserver.Tag}");

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);

        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(800, 600);
        window.SetWindowName("VTK typed events");

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        using var mouseMoveObserver = interactor.AddMouseMoveEventObserver(this.MouseMoveEventHandler);
        using var leftButtonObserver = interactor.AddLeftButtonPressEventObserver(LeftButtonPressEventHandler);
        using var keyPressObserver = interactor.AddKeyPressEventObserver(KeyPressEventHandler);

        Debug.WriteLine($"Mouse move observer tag: {mouseMoveObserver.Tag}");
        Debug.WriteLine($"Left button observer tag: {leftButtonObserver.Tag}");
        Debug.WriteLine($"Key press observer tag: {keyPressObserver.Tag}");

        window.Render();
        Debug.WriteLine("VTK Events example running. Move the mouse, click, or press a key in the render window.");
        interactor.Start();
    }

    private void ModifiedEventHandler(VtkEventArgs e)
    {
        this._modifiedCount++;
        Debug.WriteLine($"ModifiedEvent #{this._modifiedCount}: caller={e.Caller.GetType().Name}, eventId={e.EventId}");
    }

    private static void ProgressEventHandler(VtkProgressEventArgs e)
    {
        Debug.WriteLine($"ProgressEvent: caller={e.Caller.GetType().Name}, progress={e.Progress:P0}");
    }

    private void MouseMoveEventHandler(VtkMouseEventArgs e)
    {
        this._mouseMoveCount++;
        if (this._mouseMoveCount % 20 != 0) return;

        Debug.WriteLine($"MouseMoveEvent: position=({e.X}, {e.Y}), last=({e.LastX}, {e.LastY}), ctrl={e.ControlKey}, shift={e.ShiftKey}, alt={e.AltKey}");
    }

    private static void LeftButtonPressEventHandler(VtkMouseEventArgs e)
    {
        Debug.WriteLine($"LeftButtonPressEvent: position=({e.X}, {e.Y})");
    }

    private static void KeyPressEventHandler(VtkKeyEventArgs e)
    {
        Debug.WriteLine($"KeyPressEvent: keyCode='{e.KeyCode}', keySym='{e.KeySym}', repeat={e.RepeatCount}, ctrl={e.ControlKey}, shift={e.ShiftKey}, alt={e.AltKey}");
    }
}
