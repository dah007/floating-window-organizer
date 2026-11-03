using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class WindowPlacement
{
    /// <summary>
    /// Moves a window's saved position so it lies fully inside <paramref name="visibleArea"/>.
    /// Used on startup so a window saved on a now-disconnected monitor doesn't open off-screen.
    /// If the window is larger than the area, it is pinned to the area's top-left corner.
    /// </summary>
    public static (double Left, double Top) EnsureVisible(
        double left, double top, double width, double height, ScreenRect visibleArea)
    {
        var maxLeft = Math.Max(visibleArea.Left, visibleArea.Right - width);
        var maxTop = Math.Max(visibleArea.Top, visibleArea.Bottom - height);

        return (
            Math.Clamp(left, visibleArea.Left, maxLeft),
            Math.Clamp(top, visibleArea.Top, maxTop));
    }
}
