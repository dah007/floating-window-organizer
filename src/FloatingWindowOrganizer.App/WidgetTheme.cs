using System.Windows;
using System.Windows.Media;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;
using Microsoft.Win32;

namespace FloatingWindowOrganizer.App;

/// <summary>
/// Publishes the widget brushes as resources. App-level resources follow the Windows light/dark
/// setting; a widget with a custom colour puts its own brushes in its window's resources, which
/// DynamicResource lookups find first.
/// </summary>
internal static class WidgetTheme
{
    public const string TintBrushKey = "WidgetTintBrush";
    public const string BorderBrushKey = "WidgetBorderBrush";
    public const string TextBrushKey = "WidgetTextBrush";
    public const string SecondaryTextBrushKey = "WidgetSecondaryTextBrush";
    public const string TileBrushKey = "WidgetTileBrush";
    public const string TileHoverBrushKey = "WidgetTileHoverBrush";

    private static readonly string[] Keys =
    [
        TintBrushKey, BorderBrushKey, TextBrushKey, SecondaryTextBrushKey, TileBrushKey, TileHoverBrushKey,
    ];

    /// <summary>Applies the palette for the current Windows light/dark setting.</summary>
    public static void Apply(ResourceDictionary resources) =>
        Apply(resources, WindowsTheme.PaletteFor(IsWindowsLightTheme()));

    /// <summary>True when Windows apps are set to light mode (the default when the setting is missing).</summary>
    public static bool IsWindowsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(WindowsTheme.PersonalizeKeyPath);
        return WindowsTheme.IsLightTheme(key?.GetValue(WindowsTheme.AppsUseLightThemeValueName));
    }

    public static void Apply(ResourceDictionary resources, WidgetPalette palette)
    {
        resources[TintBrushKey] = ToBrush(palette.Tint);
        resources[BorderBrushKey] = ToBrush(palette.Border);
        resources[TextBrushKey] = ToBrush(palette.Text);
        resources[SecondaryTextBrushKey] = ToBrush(palette.SecondaryText);
        resources[TileBrushKey] = ToBrush(palette.Tile);
        resources[TileHoverBrushKey] = ToBrush(palette.TileHover);
    }

    /// <summary>Removes widget brushes so lookups fall back to the app-level (theme) ones.</summary>
    public static void Clear(ResourceDictionary resources)
    {
        foreach (var key in Keys)
        {
            resources.Remove(key);
        }
    }

    public static SolidColorBrush ToBrush(uint argb)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }
}
