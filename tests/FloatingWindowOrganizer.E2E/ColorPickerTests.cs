using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.TestSupport;

namespace FloatingWindowOrganizer.E2E;

[Trait("Category", "E2E")]
public sealed class ColorPickerTests
{
    [Fact]
    public void ColorPicker_ApplyingAPreset_PersistsANonDefaultColor()
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

        Assert.Null(env.ReadSettings().Windows[0].BackgroundColor);

        AppAutomation.ClickContextMenuItem(window, automation, "Color…");

        var app = Application.Attach(process.Id);
        var colorPicker = Retry.WhileNull(
            () => app.GetAllTopLevelWindows(automation).FirstOrDefault(w => w.Title == "Widget color"),
            TimeSpan.FromSeconds(5)).Result
            ?? throw new InvalidOperationException("The color picker window never appeared.");

        var bluePreset = Retry.WhileNull(
            () => colorPicker.FindFirstDescendant(cf => cf.ByName("Blue")),
            TimeSpan.FromSeconds(5)).Result
            ?? throw new InvalidOperationException("The \"Blue\" preset swatch wasn't found.");
        bluePreset.Click();

        var okButton = colorPicker.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName("OK")));
        Assert.NotNull(okButton);
        okButton!.Click();

        var backgroundColor = Retry.WhileNull(
            () => env.ReadSettings().Windows[0].BackgroundColor,
            TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(backgroundColor);

        AppAutomation.ExitViaContextMenu(window, automation, process);
    }
}
