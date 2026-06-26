using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("Callback", "ExtraExamples",
    Description = "Registers a ModifiedEvent observer and renders a cone.",
    SourceFiles = new[] { "ExtraExamples/Callback/Callback.cs" })]
internal class Callback : IExample
{
    public void Run()
    {
        using var cone = vtkConeSource.New();

        using var observer = cone.AddObserver(VtkCommandEventIds.ModifiedEvent, ObjectEventHandler);

        cone.SetHeight(3.0);
        cone.SetRadius(1.0);
        cone.SetResolution(16);

        Debug.WriteLine($"Observer tag: {observer.Tag}");

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

    private int _modifiedCount = 0;

    private void ObjectEventHandler(vtkObject caller, uint eventId)
    {
        this._modifiedCount++;
        Debug.WriteLine($"ModifiedEvent #{this._modifiedCount}: caller={caller.GetType().Name}, eventId={eventId}");
    }
}