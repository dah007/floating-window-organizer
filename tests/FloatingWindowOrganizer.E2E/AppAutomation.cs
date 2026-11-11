using System.Diagnostics;
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
    /// Finds the widget's top-level window via UIA. The widget is a WS_EX_TOOLWINDOW owned by the
    /// desktop (see DesktopAttachment), so Process.MainWindowHandle — which FlaUI's
    /// Application.GetMainWindow relies on — stays 0 forever; search the desktop's children instead.
    /// </summary>
    public static Window AttachMainWindow(Process process, UIA3Automation automation)
    {
        var window = Retry.WhileNull(
            () =>
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"The app exited (code {process.ExitCode}) before its widget window appeared.");
                }

                return automation.GetDesktop().FindFirstChild(cf =>
                    cf.ByProcessId(process.Id).And(cf.ByControlType(ControlType.Window)));
            },
            DefaultTimeout).Result;

        return window?.AsWindow()
            ?? throw new InvalidOperationException("The widget window never appeared.");
    }

    /// <summary>Right-clicks the element, waits for the popup ContextMenu, and clicks the item
    /// with the given header text.</summary>
    public static void ClickContextMenuItem(AutomationElement target, UIA3Automation automation, string header)
    {
        target.RightClick();

        var menu = Retry.WhileNull(
            () => automation.GetDesktop().FindFirstChild(cf => cf.ByControlType(ControlType.Menu)),
            DefaultTimeout).Result
            ?? throw new InvalidOperationException("The context menu never appeared.");

        var item = Retry.WhileNull(
            () => menu.FindFirstDescendant(cf => cf.ByName(header)),
            TimeSpan.FromSeconds(2)).Result
            ?? throw new InvalidOperationException($"Context menu item \"{header}\" wasn't found.");

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
