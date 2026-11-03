namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// A rectangle in device-independent units, e.g. the visible desktop area.
/// </summary>
public readonly record struct ScreenRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;

    public double Bottom => Top + Height;
}
