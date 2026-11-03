using System.Windows;
using System.Windows.Media;
using FloatingWindowOrganizer.Core.Services;
using Microsoft.Win32;

namespace FloatingWindowOrganizer.App;

/// <summary>
/// Publishes the widget brushes as app resources, picked from the current Windows light/dark setting.
/// XAML uses them through DynamicResource, so a theme change updates every widget at once.
/// </summary>
internal static class WidgetTheme
{
    public const string TintBrushKey = "WidgetTintBrush";
    public const string BorderBrushKey = "WidgetBorderBrush";
    public const string TextBrushKey = "WidgetTextBrush";
    public const string SecondaryTextBrushKey = "WidgetSecondaryTextBrush";
    public const string TileBrushKey = "WidgetTileBrush";
    public const string TileHoverBrushKey = "WidgetTileHoverBrush";

    public static void Apply(ResourceDictionary resources)
    {
        object? rawValue = null;
        using (var key = Registry.CurrentUser.OpenSubKey(WindowsTheme.PersonalizeKeyPath))
        {
            rawValue = key?.GetValue(WindowsTheme.AppsUseLightThemeValueName);
        }

        var palette = WindowsTheme.PaletteFor(WindowsTheme.IsLightTheme(rawValue));

        resources[TintBrushKey] = ToBrush(palette.Tint);
        resources[BorderBrushKey] = ToBrush(palette.Border);
        resources[TextBrushKey] = ToBrush(palette.Text);
        resources[SecondaryTextBrushKey] = ToBrush(palette.SecondaryText);
        resources[TileBrushKey] = ToBrush(palette.Tile);
        resources[TileHoverBrushKey] = ToBrush(palette.TileHover);
    }

    private static SolidColorBrush ToBrush(uint argb)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
        brush.Freeze();
        return brush;
    }
}
