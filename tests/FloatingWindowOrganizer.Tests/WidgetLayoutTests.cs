using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class WidgetLayoutTests
{
    [Theory]
    [InlineData(WidgetSize.Small, 170, 170, 2, 2)]
    [InlineData(WidgetSize.Medium, 364, 170, 4, 2)]
    [InlineData(WidgetSize.Large, 364, 382, 4, 4)]
    public void For_ReturnsIosStyleDimensions(WidgetSize size, double width, double height, int columns, int rows)
    {
        var dimensions = WidgetLayout.For(size);

        Assert.Equal(new WidgetDimensions(width, height, columns, rows), dimensions);
    }

    [Theory]
    [InlineData(WidgetSize.Small, 4)]
    [InlineData(WidgetSize.Medium, 8)]
    [InlineData(WidgetSize.Large, 16)]
    public void Capacity_IsColumnsTimesRows(WidgetSize size, int expected)
    {
        Assert.Equal(expected, WidgetLayout.For(size).Capacity);
    }

    [Theory]
    [InlineData(WidgetSize.Small)]
    [InlineData(WidgetSize.Medium)]
    [InlineData(WidgetSize.Large)]
    public void HeightFor_Collapsed_IsTitleRowHeightForEverySize(WidgetSize size)
    {
        Assert.Equal(WidgetLayout.CollapsedHeight, WidgetLayout.HeightFor(size, isCollapsed: true));
    }

    [Theory]
    [InlineData(WidgetSize.Small)]
    [InlineData(WidgetSize.Medium)]
    [InlineData(WidgetSize.Large)]
    public void HeightFor_Expanded_IsFullWidgetHeight(WidgetSize size)
    {
        Assert.Equal(WidgetLayout.For(size).Height, WidgetLayout.HeightFor(size, isCollapsed: false));
    }

    [Theory]
    [InlineData(WidgetSize.Small)]
    [InlineData(WidgetSize.Medium)]
    [InlineData(WidgetSize.Large)]
    public void CollapsedHeight_IsSmallerThanEveryExpandedSize(WidgetSize size)
    {
        Assert.True(WidgetLayout.CollapsedHeight < WidgetLayout.For(size).Height);
    }

    [Fact]
    public void For_UnknownSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WidgetLayout.For((WidgetSize)99));
    }

    [Fact]
    public void Fit_FewerItemsThanCapacity_ShowsAllWithoutOverflow()
    {
        var contents = WidgetLayout.Fit(new[] { "a", "b", "c" }, capacity: 4);

        Assert.Equal(new[] { "a", "b", "c" }, contents.Visible);
        Assert.Equal(0, contents.OverflowCount);
        Assert.False(contents.HasOverflow);
    }

    [Fact]
    public void Fit_ExactlyCapacity_ShowsAllWithoutOverflow()
    {
        var contents = WidgetLayout.Fit(new[] { "a", "b", "c", "d" }, capacity: 4);

        Assert.Equal(4, contents.Visible.Count);
        Assert.False(contents.HasOverflow);
    }

    [Fact]
    public void Fit_MoreThanCapacity_ReservesLastSlotForMoreTile()
    {
        var items = Enumerable.Range(1, 10).ToList();

        var contents = WidgetLayout.Fit(items, capacity: 4);

        Assert.Equal(new[] { 1, 2, 3 }, contents.Visible);
        Assert.Equal(7, contents.OverflowCount);
        Assert.True(contents.HasOverflow);
    }

    [Fact]
    public void Fit_OneOverCapacity_HidesTwoItemsBehindMoreTile()
    {
        var contents = WidgetLayout.Fit(new[] { "a", "b", "c", "d", "e" }, capacity: 4);

        Assert.Equal(new[] { "a", "b", "c" }, contents.Visible);
        Assert.Equal(2, contents.OverflowCount);
    }

    [Fact]
    public void Fit_EmptyFolder_ShowsNothing()
    {
        var contents = WidgetLayout.Fit(Array.Empty<string>(), capacity: 8);

        Assert.Empty(contents.Visible);
        Assert.False(contents.HasOverflow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Fit_CapacityBelowOne_Throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WidgetLayout.Fit(new[] { "a" }, capacity));
    }
}
