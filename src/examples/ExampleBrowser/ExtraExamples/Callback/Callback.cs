using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Callback", "ExtraExamples",
    Description = "Registers observers and renders a cone.",
    SourceFiles = new[] { "ExtraExamples/Callback/Callback.cs" })]
internal class Callback : IExample
{
    private int _modifiedCount = 0;

    public void Run()
    {
        using var cone = vtkConeSource.New();

        using var observer = cone.AddObserver(vtkCommand.ModifiedEvent, this.ObjectEventHandler);

        cone.SetHeight(3.0);
        cone.SetRadius(1.0);
        cone.SetResolution(16);

        Debug.WriteLine($"Observer tag: {observer.Tag}");

        using var dataObserver = cone.AddObserver(
            vtkCommand.UserEvent,
            ObjectEventDataHandler,
            clientData: "managed client data");

        var callData = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(callData, 42);
            cone.InvokeEvent(vtkCommand.UserEvent, callData);
        }
        finally
        {
            Marshal.FreeHGlobal(callData);
        }

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);

        using var renderer = vtkRenderer.New();
        renderer.AddActor(actor);

        using var window = vtkRenderWindow.New();
        window.AddRenderer(renderer);
        window.SetSize(800, 600);

        using var interactor = vtkRenderWindowInteractor.New();
        interactor.SetRenderWindow(window);

        window.Render();
        Debug.WriteLine("Callback example running. Modify events were printed before rendering.");
        interactor.Start();
    }

    private void ObjectEventHandler(vtkObject caller, uint eventId)
    {
        this._modifiedCount++;
        Debug.WriteLine($"ModifiedEvent #{this._modifiedCount}: caller={caller.GetType().Name}, eventId={eventId}");
    }

    private static void ObjectEventDataHandler(vtkObject caller, uint eventId, object? clientData, nint callData)
    {
        var value = callData == 0 ? 0 : Marshal.ReadInt32(callData);
        Debug.WriteLine($"UserEvent: caller={caller.GetType().Name}, eventId={eventId}, clientData={clientData}, callData={value}");
    }
}
