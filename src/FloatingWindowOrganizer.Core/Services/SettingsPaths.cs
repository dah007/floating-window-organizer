namespace FloatingWindowOrganizer.Core.Services;

public static class SettingsPaths
{
    public const string AppFolderName = "FloatingWindowOrganizer";
    public const string FileName = "settings.json";

    /// <summary>%APPDATA%\FloatingWindowOrganizer\settings.json, unless overridden by FWO_SETTINGS_DIR (test isolation only).</summary>
    public static string GetDefaultSettingsFilePath()
    {
        var overrideDir = Environment.GetEnvironmentVariable("FWO_SETTINGS_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return Path.Combine(overrideDir, FileName);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName,
            FileName);
    }
}
