using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public sealed class WidgetPaletteTests
{
    [Theory]
    [MemberData(nameof(Palettes))]
    public void LightLayers_AreWhiteAndTranslucent(WidgetPalette palette)
    {
        // Glare, bounce, streak, rim highlight and tile glare are light on glass: white, visible, never opaque.
        foreach (var argb in new[] { palette.Gloss, palette.GlossBounce, palette.GlossStreak, palette.TileGloss, palette.TileEdge })
        {
            Assert.InRange(argb >> 24, 1u, 0xFEu);
            Assert.Equal(0xFFFFFFu, argb & 0xFFFFFFu);
        }
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Edges_AreShadesOfTheTint_LighterOnTopDarkerBelow(WidgetPalette palette)
    {
        var (_, r, g, b) = ColorMath.Split(palette.Tint);
        var tint = ColorMath.RelativeLuminance(r, g, b);
        double Lum(uint argb) { var (_, cr, cg, cb) = ColorMath.Split(argb); return ColorMath.RelativeLuminance(cr, cg, cb); }

        Assert.True(Lum(palette.GlossRim) > tint);
        Assert.True(Lum(palette.GlossRimShade) < tint);
        Assert.Equal(WidgetPalette.EdgeAlpha, (byte)(palette.Border >> 24));
    }

    [Theory]
    [InlineData(0xFFEFE4D6u)] // cream: light glass, so the border is a darker cream
    [InlineData(0xFF5FCF7Au)] // mint
    [InlineData(0xFF1C1C1Eu)] // graphite: dark glass, so the border is a lighter grey
    [InlineData(0xFF5856D6u)] // indigo
    public void ForBackground_BorderIsAShadeOfTheColourThatStandsOutFromIt(uint argb)
    {
        var palette = WidgetPalette.ForBackground(argb);
        var (_, r, g, b) = ColorMath.Split(argb);
        var (_, br, bg, bb) = ColorMath.Split(palette.Border);
        var tint = ColorMath.RelativeLuminance(r, g, b);
        var border = ColorMath.RelativeLuminance(br, bg, bb);

        if (palette.Text == WidgetPalette.Light.Text)
        {
            Assert.True(border < tint, "light glass gets a darker edge");
        }
        else
        {
            Assert.True(border > tint, "dark glass gets a lighter edge");
        }

        // Same hue family: no channel jumps across the colour wheel.
        Assert.True(Math.Abs((br - bg) - (r - g)) <= 80 && Math.Abs((bg - bb) - (g - b)) <= 80);
    }

    [Fact]
    public void GlossVeil_IsFrostOnLightGlassAndSmokeOnDark()
    {
        // The veil pushes the glass away from the middle, so the text colour is never a close call.
        Assert.Equal(0xFFFFFFu, WidgetPalette.Light.GlossVeil & 0xFFFFFFu);
        Assert.Equal(0u, WidgetPalette.Dark.GlossVeil & 0xFFFFFFu);
        Assert.InRange(WidgetPalette.Light.GlossVeil >> 24, 1u, 0x40u); // faint: the card still shows its colour
        Assert.InRange(WidgetPalette.Dark.GlossVeil >> 24, 1u, 0x40u);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Glare_IsStrongerThanStreakAndBounce_AndRimIsTheCrispest(WidgetPalette palette)
    {
        Assert.True(palette.Gloss >> 24 > palette.GlossStreak >> 24);
        Assert.True(palette.Gloss >> 24 > palette.GlossBounce >> 24);
        Assert.True(palette.GlossRim >> 24 > palette.Gloss >> 24);
    }

    [Theory]
    [InlineData(0xFF101010u)]
    [InlineData(0xFF000000u)]
    public void ForBackground_DarkColor_UsesTheDarkGlass(uint argb)
    {
        var palette = WidgetPalette.ForBackground(argb);

        Assert.Equal(WidgetPalette.Dark.Gloss, palette.Gloss);
        Assert.Equal(WidgetPalette.Dark.GlossVeil, palette.GlossVeil);
        // The edge is a shade of this widget's own colour, not the dark theme's graphite.
        Assert.Equal(ColorMath.Mix(argb, 0xFFFFFFFF, 0.40, WidgetPalette.EdgeAlpha), palette.GlossRim);
        Assert.Equal(WidgetPalette.Dark.Text, palette.Text);
        Assert.Equal(argb, palette.Tint); // opaque colours keep their exact value
    }

    [Fact]
    public void ForBackground_PaleColor_UsesTheLightGlass()
    {
        var palette = WidgetPalette.ForBackground(0xFFF2F2F2);

        Assert.Equal(WidgetPalette.Light.Gloss, palette.Gloss);
        Assert.Equal(WidgetPalette.Light.GlossVeil, palette.GlossVeil);
        Assert.Equal(WidgetPalette.Light.Text, palette.Text);
    }

    [Fact]
    public void ForBackground_SameColour_DecidesTextByTheWallpaperBehindIt()
    {
        // Half-transparent sky blue: over snow the blend is pale (dark text), over night it is dark (white text).
        const uint sky = 0x805B9CFF;

        Assert.Equal(WidgetPalette.Light.Text, WidgetPalette.ForBackground(sky, wallpaper: 0xFFE2E9EF).Text);
        Assert.Equal(WidgetPalette.Dark.Text, WidgetPalette.ForBackground(sky, wallpaper: 0xFF0E1224).Text);
    }

    [Fact]
    public void ForBackground_CountUsesTheTextColourNotAFixedGrey()
    {
        // A fixed grey fails on coloured glass; the count follows the chosen text colour, slightly softer.
        var light = WidgetPalette.ForBackground(0xFFEFE4D6);
        var dark = WidgetPalette.ForBackground(0xFF1C1C1E);

        Assert.Equal(light.Text & 0xFFFFFFu, light.SecondaryText & 0xFFFFFFu);
        Assert.Equal(dark.Text & 0xFFFFFFu, dark.SecondaryText & 0xFFFFFFu);
        Assert.InRange(light.SecondaryText >> 24, 0xB0u, 0xFEu);
        Assert.InRange(dark.SecondaryText >> 24, 0xB0u, 0xFEu);
    }

    [Fact]
    public void GlassAlpha_RaisesOpacityWhenColourAndWallpaperAreFarApart()
    {
        // Pale cream at 72% over a dark night wallpaper: far apart, so the glass gets more opaque.
        var raised = WidgetPalette.GlassAlpha(0xB8EFE4D6, 0xFF0E1224);

        Assert.True(raised > 0xB8, $"alpha {raised:X2}");
        Assert.True(raised <= Math.Round(WidgetPalette.MaxGlassOpacity * 255));
    }

    [Fact]
    public void GlassAlpha_LeavesOpacityAloneWhenColourAndWallpaperMatch()
    {
        Assert.Equal((byte)0xB8, WidgetPalette.GlassAlpha(0xB8808080, 0xFF808080));
    }

    [Theory]
    [InlineData(0xFF000000u, 0xFFFFFFFFu)]
    [InlineData(0xFFFFFFFFu, 0xFF000000u)]
    [InlineData(0xF0FF9500u, 0xFF000000u)]
    public void GlassAlpha_NeverLowersWhatThePersonChose(uint argb, uint wallpaper)
    {
        Assert.True(WidgetPalette.GlassAlpha(argb, wallpaper) >= argb >> 24);
    }

    [Theory]
    [InlineData(0xB8EFE4D6u)] // cream
    [InlineData(0xB85FCF7Au)] // mint
    [InlineData(0xB85B9CFFu)] // sky
    [InlineData(0xB8F4A6C4u)] // rose
    [InlineData(0xB81C1C1Eu)] // graphite
    [InlineData(0xB8FFFFFFu)] // frost
    public void ForBackground_TextReadsOnTheBlendOverEveryTestWallpaper(uint argb)
    {
        // The canvas's three wallpapers: canyon, snow, night city.
        foreach (var wallpaper in new uint[] { 0xFFA14A2C, 0xFFE2E9EF, 0xFF1A1D36 })
        {
            var palette = WidgetPalette.ForBackground(argb, wallpaper);
            var (a, r, g, b) = ColorMath.Split(palette.Tint);
            var (_, wr, wg, wb) = ColorMath.Split(wallpaper);
            var alpha = a / 255.0;
            byte Over(byte c, byte w) => (byte)Math.Round((c * alpha) + (w * (1 - alpha)));
            var (_, tr, tg, tb) = ColorMath.Split(palette.Text);

            var ratio = ColorMath.ContrastRatio(
                ColorMath.RelativeLuminance(Over(r, wr), Over(g, wg), Over(b, wb)),
                ColorMath.RelativeLuminance(tr, tg, tb));

            Assert.True(ratio >= 4.5, $"Contrast {ratio:F2} for #{argb:X8} over #{wallpaper:X8}");
        }
    }

    public static TheoryData<WidgetPalette> Palettes => new() { WidgetPalette.Light, WidgetPalette.Dark };
}
