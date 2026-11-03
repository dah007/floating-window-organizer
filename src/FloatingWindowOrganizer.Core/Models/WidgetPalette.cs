namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// Widget colours as 0xAARRGGBB values. The tint is translucent so the blurred
/// wallpaper shows through, like an iOS widget.
/// </summary>
public sealed record WidgetPalette(
    uint Tint,
    uint Border,
    uint Text,
    uint SecondaryText,
    uint Tile,
    uint TileHover)
{
    public static WidgetPalette Light { get; } = new(
        Tint: 0xB8F2F2F7,
        Border: 0x1F000000,
        Text: 0xFF1C1C1E,
        SecondaryText: 0xFF6E6E73,
        Tile: 0x73FFFFFF,
        TileHover: 0xB3FFFFFF);

    public static WidgetPalette Dark { get; } = new(
        Tint: 0xB81C1C1E,
        Border: 0x26FFFFFF,
        Text: 0xFFFFFFFF,
        SecondaryText: 0xFFAEAEB2,
        Tile: 0x26FFFFFF,
        TileHover: 0x47FFFFFF);
}
