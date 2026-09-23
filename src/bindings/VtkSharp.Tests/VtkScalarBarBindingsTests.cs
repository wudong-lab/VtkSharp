namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkScalarBarBindingsTests
{
    [TestMethod]
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

        Assert.AreEqual(lookupTable.NativePointer, actor.GetLookupTable().NativePointer);
        Assert.AreEqual(3, actor.GetMaximumNumberOfColors());
        Assert.AreEqual(4, actor.GetNumberOfLabels());
        Assert.AreEqual("U, Magnitude", actor.GetTitle());
        Assert.AreEqual("%.1f", actor.GetLabelFormat());
    }

    [TestMethod]
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

        Assert.AreEqual(actor.NativePointer, widget.GetScalarBarActor().NativePointer);
        Assert.IsTrue(widget.GetSelectable());
        Assert.IsFalse(widget.GetResizable());
        Assert.IsTrue(widget.GetRepositionable());
    }
}
