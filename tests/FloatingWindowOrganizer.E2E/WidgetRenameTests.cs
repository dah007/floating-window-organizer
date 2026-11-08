using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.TestSupport;

namespace FloatingWindowOrganizer.E2E;

[Trait("Category", "E2E")]
public sealed class WidgetRenameTests
{
    [Fact]
    public void Widget_Rename_PersistsAcrossRelaunch()
    {
        const string newName = "My Renamed Widget";

        using var folder = new TempDirectory();
        File.WriteAllText(folder.Combine("a.txt"), "a");

        using var env = new IsolatedAppEnvironment();
        var process = env.Launch(widgets:
        [
            new FolderWindowSettings { FolderPath = folder.Path, Size = WidgetSize.Medium },
        ]);

        using var automation = new UIA3Automation();
        var window = AppAutomation.AttachMainWindow(process, automation);

        AppAutomation.ClickContextMenuItem(window, automation, "Rename");

        var editor = Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit)),
            TimeSpan.FromSeconds(5)).Result
            ?? throw new InvalidOperationException("The rename text box never appeared.");

        // BeginRename() already selects all the existing text and gives it keyboard focus.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(newName);
        Keyboard.Type(VirtualKeyShort.ENTER);

        var renamedTitle = Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(newName))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(renamedTitle);

        AppAutomation.ExitViaContextMenu(window, automation, process);

        var relaunched = env.Launch(seedSettings: false);
        var relaunchedWindow = AppAutomation.AttachMainWindow(relaunched, automation);

        var persistedTitle = Retry.WhileNull(
            () => relaunchedWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(newName))),
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(persistedTitle);

        AppAutomation.ExitViaContextMenu(relaunchedWindow, automation, relaunched);
    }
}
