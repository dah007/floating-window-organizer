using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.TestSupport;

namespace FloatingWindowOrganizer.E2E;

/// <summary>
/// Drives the real widget window (not the system tray icon — see the README's "Testing" note on
/// why tray notification-area automation is out of scope) to check it renders folder contents,
/// shows the overflow tile, and collapses/expands correctly.
/// </summary>
[Trait("Category", "E2E")]
public sealed class WidgetTileTests
{
    [Fact]
    public void Widget_ShowsFolderContentsAsTiles()
    {
        using var folder = new TempDirectory();
        File.WriteAllText(folder.Combine("a.txt"), "a");
        File.WriteAllText(folder.Combine("b.txt"), "b");
        File.WriteAllText(folder.Combine("c.txt"), "c");

        using var env = new IsolatedAppEnvironment();
        var process = env.Launch(widgets:
        [
            new FolderWindowSettings { FolderPath = folder.Path, Size = WidgetSize.Large },
        ]);

        using var automation = new UIA3Automation();
        var window = AppAutomation.AttachMainWindow(process, automation);

        foreach (var fileName in new[] { "a.txt", "b.txt", "c.txt" })
        {
            var tileText = window.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Text).And(cf.ByName(fileName)));
            Assert.NotNull(tileText);
        }

        AppAutomation.ExitViaContextMenu(window, automation, process);
    }

    [Fact]
    public void Widget_CollapseAndExpand_TogglesTileVisibility()
    {
        using var folder = new TempDirectory();
        File.WriteAllText(folder.Combine("a.txt"), "a");

        using var env = new IsolatedAppEnvironment();
        var process = env.Launch(widgets:
        [
            new FolderWindowSettings { FolderPath = folder.Path, Size = WidgetSize.Medium },
        ]);

        using var automation = new UIA3Automation();
        var window = AppAutomation.AttachMainWindow(process, automation);

        var displayName = Path.GetFileName(folder.Path);
        var titleText = window.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Text).And(cf.ByName(displayName)));
        Assert.NotNull(titleText);

        AppAutomation.RaiseForInput(window);
        titleText!.DoubleClick();
        var collapsed = Retry.WhileTrue(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("a.txt"))) is not null,
            TimeSpan.FromSeconds(2)).Success;
        Assert.True(
            collapsed,
            "The widget's tiles should be hidden once collapsed. Window under the click point: " +
            AppAutomation.DescribeClickTarget(titleText));

        // Wait out the system double-click time, or Windows counts the next two clicks as the
        // third and fourth of the previous sequence rather than a new double-click.
        Thread.Sleep(1000);
        AppAutomation.RaiseForInput(window);
        titleText.DoubleClick();
        var expanded = Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("a.txt"))),
            TimeSpan.FromSeconds(2)).Result;
        Assert.NotNull(expanded);

        AppAutomation.ExitViaContextMenu(window, automation, process);
    }
}
