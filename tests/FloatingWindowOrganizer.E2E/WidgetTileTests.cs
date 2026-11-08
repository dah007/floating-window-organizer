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
    public void Widget_OverflowTileShowsRemainingCount()
    {
        using var folder = new TempDirectory();
        for (var i = 0; i < 10; i++)
        {
            File.WriteAllText(folder.Combine($"file-{i:00}.txt"), string.Empty);
        }

        using var env = new IsolatedAppEnvironment();
        // Small = 2x2 = capacity 4, so 3 files show and the 4th slot is the "+7" overflow tile.
        var process = env.Launch(widgets:
        [
            new FolderWindowSettings { FolderPath = folder.Path, Size = WidgetSize.Small },
        ]);

        using var automation = new UIA3Automation();
        var window = AppAutomation.AttachMainWindow(process, automation);

        var overflowText = window.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Text).And(cf.ByName("+7")));
        Assert.NotNull(overflowText);

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

        titleText!.DoubleClick();
        var collapsed = Retry.WhileTrue(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("a.txt"))) is not null,
            TimeSpan.FromSeconds(2)).Success;
        Assert.True(collapsed, "The widget's tiles should be hidden once collapsed.");

        titleText.DoubleClick();
        var expanded = Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("a.txt"))),
            TimeSpan.FromSeconds(2)).Result;
        Assert.NotNull(expanded);

        AppAutomation.ExitViaContextMenu(window, automation, process);
    }
}
