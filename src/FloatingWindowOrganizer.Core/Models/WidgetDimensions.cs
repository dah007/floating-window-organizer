namespace FloatingWindowOrganizer.Core.Models;

/// <summary>Outer size of a widget (device-independent units) and its icon grid.</summary>
public readonly record struct WidgetDimensions(double Width, double Height, int Columns, int Rows)
{
    public int Capacity => Columns * Rows;
}
