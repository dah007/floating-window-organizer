namespace FloatingWindowOrganizer.Core.Services;

public static class SettingsPaths
{
    public const string AppFolderName = "FloatingWindowOrganizer";
    public const string FileName = "settings.json";

    /// <summary>%APPDATA%\FloatingWindowOrganizer\settings.json</summary>
    public static string GetDefaultSettingsFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName,
            FileName);
}
