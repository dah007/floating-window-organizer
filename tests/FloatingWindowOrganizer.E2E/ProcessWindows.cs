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
