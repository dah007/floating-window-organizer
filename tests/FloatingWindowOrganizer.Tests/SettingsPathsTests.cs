using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class SettingsPathsTests
{
    [Fact]
    public void DefaultPath_IsInsideAppDataUnderAppFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = SettingsPaths.GetDefaultSettingsFilePath();

        Assert.Equal(Path.Combine(appData, "FloatingWindowOrganizer", "settings.json"), path);
    }
}
