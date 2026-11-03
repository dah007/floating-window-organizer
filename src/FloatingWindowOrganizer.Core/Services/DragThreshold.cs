namespace FloatingWindowOrganizer.Core.Services;

public static class DragThreshold
{
    /// <summary>
    /// True when the pointer has moved far enough from where the button was pressed
    /// for the gesture to count as a drag rather than a click.
    /// </summary>
    public static bool IsExceeded(double deltaX, double deltaY, double minimumX, double minimumY) =>
        Math.Abs(deltaX) > minimumX || Math.Abs(deltaY) > minimumY;
}
