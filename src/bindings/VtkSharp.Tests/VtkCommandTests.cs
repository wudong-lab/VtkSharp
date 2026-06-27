namespace VtkSharp.Tests;

public sealed class VtkCommandTests
{
    [Fact]
    public void EventIds_MatchVtkCommandEventOrder()
    {
        Assert.Equal(0u, vtkCommand.NoEvent);
        Assert.Equal(1u, vtkCommand.AnyEvent);
        Assert.Equal(8u, vtkCommand.StartPickEvent);
        Assert.Equal(33u, vtkCommand.ModifiedEvent);
        Assert.Equal(57u, vtkCommand.StartAnimationCueEvent);
        Assert.Equal(84u, vtkCommand.ComputeVisiblePropBoundsEvent);
        Assert.Equal(124u, vtkCommand.LeftButtonDoubleClickEvent);
        Assert.Equal(136u, vtkCommand.Elevation3DEvent);
        Assert.Equal(1000u, vtkCommand.UserEvent);
    }
}
