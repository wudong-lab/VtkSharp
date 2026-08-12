namespace VtkSharp.Tests;

public sealed class VtkScalarBarBindingsTests
{
    [Fact]
    public void ScalarBarActor_UsesConfiguredLookupTableAndLabels()
    {
        using var lookupTable = vtkLookupTable.New();
        lookupTable.SetNumberOfTableValues(3);
        lookupTable.SetTableRange(-1.0, 2.0);
        lookupTable.SetTableValue(0, 0.0, 0.0, 1.0, 1.0);
        lookupTable.SetTableValue(1, 0.0, 1.0, 0.0, 1.0);
        lookupTable.SetTableValue(2, 1.0, 0.0, 0.0, 1.0);
        lookupTable.Build();

        using var actor = vtkScalarBarActor.New();
        actor.SetLookupTable(lookupTable);
        actor.SetMaximumNumberOfColors(3);
        actor.SetNumberOfLabels(4);
        actor.SetTitle("U, Magnitude");
        actor.SetLabelFormat("%.1f");

        Assert.Equal(lookupTable.NativePointer, actor.GetLookupTable().NativePointer);
        Assert.Equal(3, actor.GetMaximumNumberOfColors());
        Assert.Equal(4, actor.GetNumberOfLabels());
        Assert.Equal("U, Magnitude", actor.GetTitle());
        Assert.Equal("%.1f", actor.GetLabelFormat());
    }

    [Fact]
    public void ScalarBarWidget_AcceptsScalarBarActorAndInteractionOptions()
    {
        using var actor = vtkScalarBarActor.New();
        using var representation = vtkScalarBarRepresentation.New();
        representation.SetScalarBarActor(actor);
        representation.SetPosition(0.1, 0.1);
        representation.SetPosition2(0.2, 0.8);

        using var widget = vtkScalarBarWidget.New();
        widget.SetRepresentation(representation);
        widget.SetScalarBarActor(actor);
        widget.SelectableOn();
        widget.ResizableOff();
        widget.RepositionableOn();

        Assert.Equal(actor.NativePointer, widget.GetScalarBarActor().NativePointer);
        Assert.True(widget.GetSelectable());
        Assert.False(widget.GetResizable());
        Assert.True(widget.GetRepositionable());
    }
}
