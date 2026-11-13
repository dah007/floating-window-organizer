using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class WidgetLayout
{
    /// <summary>Corner radius of the widget card, in device-independent units.</summary>
    public const double CornerRadius = 22;

    /// <summary>
    /// Height of a widget collapsed to its title row: 2 border + 11 padding + 18 title + 11 padding + 2 border.
    /// Must match the paddings in FolderWindow.xaml.
    /// </summary>
    public const double CollapsedHeight = 44;

    /// <summary>
    /// Sizes based on iOS widget proportions. Extra large keeps Large's cell size (about 83.5 x 83 per
    /// tile): 30 of border and padding across, plus 26 of border and padding and 24 of title row down.
    /// </summary>
    public static WidgetDimensions For(WidgetSize size) => size switch
    {
        WidgetSize.Small => new WidgetDimensions(170, 170, Columns: 2, Rows: 2),
        WidgetSize.Medium => new WidgetDimensions(364, 170, Columns: 4, Rows: 2),
        WidgetSize.Large => new WidgetDimensions(364, 382, Columns: 4, Rows: 4),
        WidgetSize.ExtraLarge => new WidgetDimensions(448, 465, Columns: 5, Rows: 5),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown widget size."),
    };

    /// <summary>Window height for a widget size, expanded or collapsed to its title row.</summary>
    public static double HeightFor(WidgetSize size, bool isCollapsed) =>
        isCollapsed ? CollapsedHeight : For(size).Height;

    /// <summary>
    /// Picks the items to show. If everything fits, all items are shown. Otherwise the
    /// first <c>capacity - 1</c> items are shown and the last slot is reserved for a "+N" tile.
    /// </summary>
    public static WidgetContents<T> Fit<T>(IReadOnlyList<T> items, int capacity)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        if (items.Count <= capacity)
        {
            return new WidgetContents<T>(items, 0);
        }

        var visibleCount = capacity - 1;
        return new WidgetContents<T>(items.Take(visibleCount).ToList(), items.Count - visibleCount);
    }
}
