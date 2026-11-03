namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// The items that fit in a widget. When <see cref="OverflowCount"/> is above zero,
/// the widget's last slot is a "+N" tile representing that many hidden items.
/// </summary>
public sealed record WidgetContents<T>(IReadOnlyList<T> Visible, int OverflowCount)
{
    public bool HasOverflow => OverflowCount > 0;
}
