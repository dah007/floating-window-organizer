using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class WidgetLayout
{
    /// <summary>Corner radius of the widget card, in device-independent units.</summary>
    public const double CornerRadius = 22;

    /// <summary>Sizes based on iOS widget proportions.</summary>
    public static WidgetDimensions For(WidgetSize size) => size switch
    {
        WidgetSize.Small => new WidgetDimensions(170, 170, Columns: 2, Rows: 2),
        WidgetSize.Medium => new WidgetDimensions(364, 170, Columns: 4, Rows: 2),
        WidgetSize.Large => new WidgetDimensions(364, 382, Columns: 4, Rows: 4),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown widget size."),
    };

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
