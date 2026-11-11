using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace FloatingWindowOrganizer.E2E;

/// <summary>Small FlaUI helpers shared by the E2E tests: attaching to the launched app, and
/// driving the widget's right-click context menu (a WPF ContextMenu, which UIA surfaces as its
/// own top-level popup rather than a child of the window that opened it).</summary>
internal static class AppAutomation
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Finds the widget's window. The widget is a WS_EX_TOOLWINDOW owned by the desktop (see
    /// DesktopAttachment), so Process.MainWindowHandle — which FlaUI's Application.GetMainWindow
    /// relies on — stays 0 forever, and where UIA places a Progman-owned window in its tree varies
    /// by machine. So find the HWND with EnumWindows and wrap it with UIA directly.
    /// </summary>
    public static Window AttachMainWindow(Process process, UIA3Automation automation) =>
        FindProcessWindow(process, automation, "widget window", DefaultTimeout, _ => true);

    /// <summary>Finds a visible window of the app with the given title (e.g. a dialog).</summary>
    public static Window FindWindowByTitle(Process process, UIA3Automation automation, string title, TimeSpan timeout) =>
        FindProcessWindow(process, automation, $"\"{title}\" window", timeout, w => w.Title == title);

    private static Window FindProcessWindow(
        Process process,
        UIA3Automation automation,
        string description,
        TimeSpan timeout,
        Func<ProcessWindows.Info, bool> match)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The app exited (code {process.ExitCode}) before its {description} appeared.");
            }

            var hwnd = ProcessWindows.FindVisibleWpfWindows(process.Id).FirstOrDefault(match);
            if (hwnd is not null)
            {
                try
                {
                    // UIA can briefly refuse a window that's still initializing; retry until the deadline.
                    return automation.FromHandle(hwnd.Handle).AsWindow();
                }
                catch (Exception ex) when (ex is COMException or System.ComponentModel.Win32Exception)
                {
                    lastError = ex;
                }
            }

            Thread.Sleep(200);
        }

        var seen = ProcessWindows.List(process.Id);
        throw new InvalidOperationException(
            $"The {description} never appeared. Windows owned by the app process: " +
            (seen.Count == 0 ? "(none)" : string.Join("; ", seen)),
            lastError);
    }

    /// <summary>
    /// Raises the window containing <paramref name="element"/> above everything (without
    /// activating it) so synthetic mouse input reaches it. Call before clicking a widget.
    /// </summary>
    public static void RaiseForInput(AutomationElement element)
    {
        for (var current = element; current is not null; current = current.Parent)
        {
            var hwnd = current.Properties.NativeWindowHandle.ValueOrDefault;
            if (hwnd != IntPtr.Zero)
            {
                ProcessWindows.RaiseToTop(hwnd);
                Thread.Sleep(100);
                return;
            }
        }
    }

    /// <summary>Which window actually sits under the element's click point (for failure messages).</summary>
    public static string DescribeClickTarget(AutomationElement element)
    {
        try
        {
            var point = element.GetClickablePoint();
            return ProcessWindows.DescribeWindowAt(point.X, point.Y);
        }
        catch (Exception ex)
        {
            return $"(couldn't get a clickable point: {ex.Message})";
        }
    }

    /// <summary>
    /// Right-clicks the element, waits for the popup ContextMenu, and clicks the item with the
    /// given header text. The popup is found as a new top-level window of the app's process (by
    /// HWND), since where UIA places a desktop-owned widget's popup in its tree varies by machine.
    /// </summary>
    public static void ClickContextMenuItem(AutomationElement target, UIA3Automation automation, string header)
    {
        var processId = target.Properties.ProcessId.Value;
        var before = ProcessWindows.FindVisibleWpfWindows(processId).Select(w => w.Handle).ToHashSet();

        RaiseForInput(target);
        target.RightClick();

        var item = Retry.WhileNull(
            () => ProcessWindows.FindVisibleWpfWindows(processId)
                .Where(w => !before.Contains(w.Handle))
                .Select(w => automation.FromHandle(w.Handle).FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.MenuItem).And(cf.ByName(header))))
                .FirstOrDefault(found => found is not null),
            DefaultTimeout,
            ignoreException: true).Result
            ?? throw new InvalidOperationException(
                $"Context menu item \"{header}\" never appeared. Window under the click point: " +
                DescribeClickTarget(target) + ". Windows owned by the app process: " +
                string.Join("; ", ProcessWindows.List(processId)));

        item.Click();
    }

    /// <summary>Opens the widget's context menu and clicks Exit, then waits for the process to
    /// exit cleanly so every E2E test ends with a full, self-contained app lifecycle.</summary>
    public static void ExitViaContextMenu(AutomationElement target, UIA3Automation automation, Process process)
    {
        ClickContextMenuItem(target, automation, "Exit");

        var exited = process.WaitForExit((int)DefaultTimeout.TotalMilliseconds);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
        }

        Assert.True(exited, "The app didn't exit after Exit was clicked from the widget's context menu.");
        Assert.Equal(0, process.ExitCode);
    }
}
