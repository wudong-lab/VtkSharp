namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkCommandTests
{
    [TestMethod]
    public void EventIds_MatchVtkCommandEventOrder()
    {
        Assert.AreEqual(0u, vtkCommand.NoEvent);
        Assert.AreEqual(1u, vtkCommand.AnyEvent);
        Assert.AreEqual(8u, vtkCommand.StartPickEvent);
        Assert.AreEqual(33u, vtkCommand.ModifiedEvent);
        Assert.AreEqual(57u, vtkCommand.StartAnimationCueEvent);
        Assert.AreEqual(84u, vtkCommand.ComputeVisiblePropBoundsEvent);
        Assert.AreEqual(124u, vtkCommand.LeftButtonDoubleClickEvent);
        Assert.AreEqual(136u, vtkCommand.Elevation3DEvent);
        Assert.AreEqual(1000u, vtkCommand.UserEvent);
    }
}
