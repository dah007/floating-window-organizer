using System.Runtime.InteropServices;
using System.Text;

namespace FloatingWindowOrganizer.E2E;

/// <summary>
/// Lists a process's top-level windows straight from Win32. EnumWindows includes owned windows
/// (like the Progman-owned widgets), so this doesn't depend on where UI Automation happens to
/// place them in its tree, which varies between machines and CI runners.
/// </summary>
internal static class ProcessWindows
{
    public sealed record Info(IntPtr Handle, string ClassName, string Title, bool Visible, int Width, int Height)
    {
        public override string ToString() =>
            $"0x{Handle:X} class={ClassName} title=\"{Title}\" visible={Visible} size={Width}x{Height}";
    }

    public static List<Info> List(int processId)
    {
        var windows = new List<Info>();
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == processId)
            {
                var className = new StringBuilder(256);
                GetClassName(hwnd, className, className.Capacity);
                var title = new StringBuilder(256);
                GetWindowText(hwnd, title, title.Capacity);
                GetWindowRect(hwnd, out var rect);
                windows.Add(new Info(
                    hwnd,
                    className.ToString(),
                    title.ToString(),
                    IsWindowVisible(hwnd),
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top));
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    /// <summary>The process's visible, non-empty WPF windows, in Z-order.</summary>
    public static IEnumerable<Info> FindVisibleWpfWindows(int processId) =>
        List(processId).Where(w =>
            w.Visible && w.Width > 0 && w.Height > 0 && w.ClassName.StartsWith("HwndWrapper", StringComparison.Ordinal));

    /// <summary>
    /// Raises a window above everything without activating it. The app deliberately keeps widgets
    /// at the bottom of the Z-order (desktop layer), so on a CI runner other windows can cover them
    /// and swallow synthetic mouse input. The app's next SendToBottom (HWND_BOTTOM) clears this.
    /// </summary>
    public static void RaiseToTop(IntPtr hwnd) =>
        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);

    /// <summary>Describes the top-level window under a screen point, for failure messages.</summary>
    public static string DescribeWindowAt(int x, int y)
    {
        var hwnd = WindowFromPoint(new Point { X = x, Y = y });
        if (hwnd == IntPtr.Zero)
        {
            return $"({x},{y}): no window";
        }

        var root = GetAncestor(hwnd, GaRoot);
        GetWindowThreadProcessId(root, out var pid);
        var className = new StringBuilder(256);
        GetClassName(root, className, className.Capacity);
        var title = new StringBuilder(256);
        GetWindowText(root, title, title.Capacity);
        return $"({x},{y}): 0x{root:X} pid={pid} class={className} title=\"{title}\"";
    }

    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint GaRoot = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
}
