using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// Widget colours as 0xAARRGGBB values. The tint is translucent so the blurred wallpaper shows
/// through, like an iOS widget.
/// <para>
/// The glossy glass layers (drawn when a widget's Glossy option is on) are the same recipe on every
/// colour; only their strength follows the palette:
/// GlossVeil is a faint frost (white, light glass) or smoke (black, dark glass) over the tint that
/// pushes mid-tones clearly to one side so the text stays readable; Gloss is the peak of the glare
/// along the top, GlossBounce the faint light coming back up from the bottom edge, GlossStreak the
/// diagonal streak across the pane; GlossRim and GlossRimShade are the top and bottom of the glossy
/// edge, a lighter and a darker shade of the tint; TileGloss and TileEdge give each icon tile its own small glare and rim.
/// </para>
/// </summary>
public sealed record WidgetPalette(
    uint Tint,
    uint Border,
    uint Text,
    uint SecondaryText,
    uint Tile,
    uint TileHover,
    uint GlossVeil,
    uint Gloss,
    uint GlossBounce,
    uint GlossStreak,
    uint GlossRim,
    uint GlossRimShade,
    uint TileGloss,
    uint TileEdge)
{
    /// <summary>Glass opacity is never raised above this, so the wallpaper always shows through a little.</summary>
    public const double MaxGlassOpacity = 0.92;

    /// <summary>How much opacity is added per unit of luminance difference between colour and wallpaper.</summary>
    public const double OpacityPerLuminance = 0.3;

    /// <summary>Stand-in for the wallpaper when its colour isn't known: mid-grey.</summary>
    public const uint DefaultWallpaper = 0xFF808080;

    public static WidgetPalette Light { get; } = WithEdges(new(
        Tint: 0xB8F2F2F7,
        Border: 0, // from the tint, see WithEdges
        Text: 0xFF1C1C1E,
        SecondaryText: 0xFF6E6E73,
        Tile: 0x73FFFFFF,
        TileHover: 0xB3FFFFFF,
        GlossVeil: 0x29FFFFFF,
        Gloss: 0x59FFFFFF,
        GlossBounce: 0x15FFFFFF,
        GlossStreak: 0x19FFFFFF,
        GlossRim: 0, // from the tint, see WithEdges
        GlossRimShade: 0, // from the tint, see WithEdges
        TileGloss: 0x59FFFFFF,
        TileEdge: 0xBFFFFFFF), darkText: true);

    public static WidgetPalette Dark { get; } = WithEdges(new(
        Tint: 0xB81C1C1E,
        Border: 0, // from the tint, see WithEdges
        Text: 0xFFFFFFFF,
        SecondaryText: 0xFFAEAEB2,
        Tile: 0x26FFFFFF,
        TileHover: 0x47FFFFFF,
        GlossVeil: 0x29000000,
        Gloss: 0x47FFFFFF,
        GlossBounce: 0x11FFFFFF,
        GlossStreak: 0x14FFFFFF,
        GlossRim: 0, // from the tint, see WithEdges
        GlossRimShade: 0, // from the tint, see WithEdges
        TileGloss: 0x24FFFFFF,
        TileEdge: 0x2EFFFFFF), darkText: false);

    /// <summary>
    /// Palette for a widget with a custom colour, so it reads on any colour and any wallpaper:
    /// <list type="number">
    /// <item>The further the colour's brightness is from the wallpaper's, the more opaque the glass
    /// (<see cref="GlassAlpha"/>), so the colour, not the wallpaper, decides what the text sits on.
    /// Opacity is only ever raised, never lowered below what the person chose.</item>
    /// <item>The colour is blended over the wallpaper's average colour (mid-grey when unknown), and the
    /// text is whichever of dark or white contrasts more with that blend.</item>
    /// <item>Border, tiles and gloss come from the matching theme palette; the count uses the text
    /// colour at reduced strength rather than a fixed grey, which can fail on coloured glass.</item>
    /// </list>
    /// </summary>
    public static WidgetPalette ForBackground(uint argb, uint? wallpaper = null)
    {
        var backdrop = wallpaper ?? DefaultWallpaper;
        var alpha = GlassAlpha(argb, backdrop);
        var (_, r, g, b) = ColorMath.Split(argb);
        var tint = ColorMath.ToArgb(alpha, r, g, b);

        return ColorMath.PrefersDarkText(tint, backdrop)
            ? WithEdges(Light with { Tint = tint, SecondaryText = 0xBD1C1C1E }, darkText: true)
            : WithEdges(Dark with { Tint = tint, SecondaryText = 0xCCFFFFFF }, darkText: false);
    }

    /// <summary>How opaque the card edge is: clearly visible, but still a little glassy.</summary>
    public const byte EdgeAlpha = 0xE6;

    /// <summary>
    /// The card edge, in shades of the tint so it always belongs to the card's colour. Light glass gets a
    /// darker shade, dark glass a lighter one, so the edge stands out from the card in both. The glossy
    /// edge runs from a lighter shade along the top to a darker one along the bottom.
    /// </summary>
    public static WidgetPalette WithEdges(WidgetPalette palette, bool darkText)
    {
        ArgumentNullException.ThrowIfNull(palette);
        var tint = palette.Tint;

        return palette with
        {
            Border = ColorMath.Mix(tint, darkText ? 0xFF000000 : 0xFFFFFFFF, darkText ? 0.22 : 0.30, EdgeAlpha),
            GlossRim = ColorMath.Mix(tint, 0xFFFFFFFF, darkText ? 0.55 : 0.40, EdgeAlpha),
            GlossRimShade = ColorMath.Mix(tint, 0xFF000000, darkText ? 0.30 : 0.15, EdgeAlpha),
        };
    }

    /// <summary>
    /// The colour's alpha, raised by <see cref="OpacityPerLuminance"/> per unit of luminance difference
    /// between the colour and the wallpaper, up to <see cref="MaxGlassOpacity"/>. Never lowered.
    /// </summary>
    public static byte GlassAlpha(uint argb, uint wallpaper)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        var (_, wr, wg, wb) = ColorMath.Split(wallpaper);
        var difference = Math.Abs(ColorMath.RelativeLuminance(r, g, b) - ColorMath.RelativeLuminance(wr, wg, wb));

        var chosen = a / 255.0;
        var raised = Math.Min(MaxGlassOpacity, chosen + (OpacityPerLuminance * difference));
        return (byte)Math.Round(Math.Max(chosen, raised) * 255);
    }
}
