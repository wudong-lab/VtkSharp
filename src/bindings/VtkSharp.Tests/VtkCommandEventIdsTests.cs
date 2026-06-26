namespace VtkSharp.Tests;

public sealed class VtkCommandEventIdsTests
{
    [Fact]
    public void EventIds_MatchVtkCommandEventOrder()
    {
        Assert.Equal(0u, VtkCommandEventIds.NoEvent);
        Assert.Equal(1u, VtkCommandEventIds.AnyEvent);
        Assert.Equal(8u, VtkCommandEventIds.StartPickEvent);
        Assert.Equal(33u, VtkCommandEventIds.ModifiedEvent);
        Assert.Equal(57u, VtkCommandEventIds.StartAnimationCueEvent);
        Assert.Equal(84u, VtkCommandEventIds.ComputeVisiblePropBoundsEvent);
        Assert.Equal(124u, VtkCommandEventIds.LeftButtonDoubleClickEvent);
        Assert.Equal(136u, VtkCommandEventIds.Elevation3DEvent);
        Assert.Equal(1000u, VtkCommandEventIds.UserEvent);
    }
}
