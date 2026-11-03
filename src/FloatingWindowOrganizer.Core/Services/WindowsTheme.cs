using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class WindowsTheme
{
    /// <summary>Registry key (under HKEY_CURRENT_USER) holding the app light/dark setting.</summary>
    public const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    /// <summary>
    /// Interprets the raw AppsUseLightTheme registry value: 0 means dark, any other number means light.
    /// A missing or unexpected value means light, which is the Windows default.
    /// </summary>
    public static bool IsLightTheme(object? appsUseLightThemeValue) =>
        appsUseLightThemeValue switch
        {
            int value => value != 0,
            long value => value != 0,
            _ => true,
        };

    public static WidgetPalette PaletteFor(bool isLightTheme) =>
        isLightTheme ? WidgetPalette.Light : WidgetPalette.Dark;
}
