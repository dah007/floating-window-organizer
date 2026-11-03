using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class WindowPlacementTests
{
    private static readonly ScreenRect Desktop = new(0, 0, 1920, 1080);

    [Fact]
    public void WindowAlreadyVisible_IsUnchanged()
    {
        var result = WindowPlacement.EnsureVisible(100, 200, 300, 400, Desktop);

        Assert.Equal((100d, 200d), result);
    }

    [Fact]
    public void WindowPastRightAndBottom_IsPulledBackOnScreen()
    {
        var result = WindowPlacement.EnsureVisible(5000, 5000, 300, 400, Desktop);

        Assert.Equal((1620d, 680d), result);
    }

    [Fact]
    public void WindowPastLeftAndTop_IsPulledBackOnScreen()
    {
        var result = WindowPlacement.EnsureVisible(-500, -500, 300, 400, Desktop);

        Assert.Equal((0d, 0d), result);
    }

    [Fact]
    public void WindowLargerThanArea_IsPinnedToTopLeft()
    {
        var result = WindowPlacement.EnsureVisible(300, 300, 3000, 2000, Desktop);

        Assert.Equal((0d, 0d), result);
    }

    [Fact]
    public void MonitorLeftOfPrimary_NegativeCoordinatesAreAllowed()
    {
        // Two 1920x1080 monitors, the second one placed to the left of the primary.
        var virtualScreen = new ScreenRect(-1920, 0, 3840, 1080);

        var result = WindowPlacement.EnsureVisible(-1500, 100, 300, 400, virtualScreen);

        Assert.Equal((-1500d, 100d), result);
    }
}
