using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class DragThresholdTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 4)]     // exactly at the threshold is still a click
    [InlineData(-4, 3)]
    public void SmallMovement_IsNotADrag(double dx, double dy)
    {
        Assert.False(DragThreshold.IsExceeded(dx, dy, 4, 4));
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    [InlineData(-5, 0)]
    [InlineData(0, -5)]
    public void MovementPastThresholdInEitherDirection_IsADrag(double dx, double dy)
    {
        Assert.True(DragThreshold.IsExceeded(dx, dy, 4, 4));
    }
}
