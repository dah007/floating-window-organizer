using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class ColorMathTests
{
    [Theory]
    [InlineData(0, 1, 1, 255, 0, 0)]       // red
    [InlineData(60, 1, 1, 255, 255, 0)]    // yellow
    [InlineData(120, 1, 1, 0, 255, 0)]     // green
    [InlineData(180, 1, 1, 0, 255, 255)]   // cyan
    [InlineData(240, 1, 1, 0, 0, 255)]     // blue
    [InlineData(300, 1, 1, 255, 0, 255)]   // magenta
    [InlineData(0, 0, 1, 255, 255, 255)]   // white
    [InlineData(0, 0, 0, 0, 0, 0)]         // black
    [InlineData(0, 0, 0.5, 128, 128, 128)] // grey
    public void HsvToRgb_KnownColours(double h, double s, double v, byte r, byte g, byte b)
    {
        Assert.Equal((r, g, b), ColorMath.HsvToRgb(h, s, v));
    }

    [Fact]
    public void HsvToRgb_WrapsHueAndClampsInputs()
    {
        Assert.Equal(ColorMath.HsvToRgb(0, 1, 1), ColorMath.HsvToRgb(360, 1, 1));
        Assert.Equal(ColorMath.HsvToRgb(300, 1, 1), ColorMath.HsvToRgb(-60, 1, 1));
        Assert.Equal(ColorMath.HsvToRgb(0, 1, 1), ColorMath.HsvToRgb(0, 2, 5));
    }

    [Theory]
    [InlineData(255, 0, 0, 0, 1, 1)]
    [InlineData(0, 255, 0, 120, 1, 1)]
    [InlineData(0, 0, 255, 240, 1, 1)]
    [InlineData(255, 0, 255, 300, 1, 1)]
    [InlineData(255, 255, 255, 0, 0, 1)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    public void RgbToHsv_KnownColours(byte r, byte g, byte b, double h, double s, double v)
    {
        var (hue, saturation, value) = ColorMath.RgbToHsv(r, g, b);

        Assert.Equal(h, hue, 3);
        Assert.Equal(s, saturation, 3);
        Assert.Equal(v, value, 3);
    }

    [Theory]
    [InlineData(0x00, 0x7A, 0xFF)]
    [InlineData(0xFF, 0x95, 0x00)]
    [InlineData(0x34, 0xC7, 0x59)]
    [InlineData(0x1C, 0x1C, 0x1E)]
    [InlineData(0xA2, 0x84, 0x5E)]
    public void RgbToHsvToRgb_RoundTrips(byte r, byte g, byte b)
    {
        var (h, s, v) = ColorMath.RgbToHsv(r, g, b);

        Assert.Equal((r, g, b), ColorMath.HsvToRgb(h, s, v));
    }

    [Fact]
    public void ToArgbAndSplit_AreInverse()
    {
        var argb = ColorMath.ToArgb(0xB8, 0x12, 0x34, 0x56);

        Assert.Equal(0xB8123456u, argb);
        Assert.Equal(((byte)0xB8, (byte)0x12, (byte)0x34, (byte)0x56), ColorMath.Split(argb));
    }

    [Fact]
    public void Hex_FormatsWithAndWithoutAlpha()
    {
        Assert.Equal("#B8FF9500", ColorMath.ToArgbHex(0xB8FF9500));
        Assert.Equal("#FF9500", ColorMath.ToRgbHex(0xB8FF9500));
    }

    [Theory]
    [InlineData("#FF9500", 0xFFFF9500u, false)]
    [InlineData("ff9500", 0xFFFF9500u, false)]
    [InlineData("  #ff9500  ", 0xFFFF9500u, false)]
    [InlineData("#B8FF9500", 0xB8FF9500u, true)]
    public void TryParseHex_Valid(string text, uint expected, bool expectedHasAlpha)
    {
        Assert.True(ColorMath.TryParseHex(text, out var argb, out var hasAlpha));
        Assert.Equal(expected, argb);
        Assert.Equal(expectedHasAlpha, hasAlpha);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#FFF")]
    [InlineData("#GG9500")]
    [InlineData("#FF95001")]
    [InlineData("orange")]
    [InlineData("+F9500")]
    public void TryParseHex_Invalid(string? text)
    {
        Assert.False(ColorMath.TryParseHex(text, out _, out _));
    }

    [Fact]
    public void ContrastRatio_BlackOnWhite_Is21()
    {
        Assert.Equal(21, ColorMath.ContrastRatio(ColorMath.RelativeLuminance(255, 255, 255), ColorMath.RelativeLuminance(0, 0, 0)), 3);
    }

    [Theory]
    [InlineData(0xFFFFFFFFu)] // white
    [InlineData(0xFFFFCC00u)] // yellow
    [InlineData(0xFF34C759u)] // green
    [InlineData(0xFFF2F2F7u)] // light grey
    public void LightBackgrounds_GetDarkText(uint argb)
    {
        Assert.True(ColorMath.PrefersDarkText(argb));
    }

    [Theory]
    [InlineData(0xFF000000u)] // black
    [InlineData(0xFF1C1C1Eu)] // graphite
    [InlineData(0xFF5856D6u)] // indigo
    [InlineData(0xFF002060u)] // navy
    public void DarkBackgrounds_GetWhiteText(uint argb)
    {
        Assert.False(ColorMath.PrefersDarkText(argb));
    }

    [Fact]
    public void TranslucentColour_IsJudgedOverMidGrey()
    {
        // Fully transparent black is just the assumed mid-grey wallpaper, which reads better with
        // dark text; opaque black needs white.
        Assert.True(ColorMath.PrefersDarkText(0x00000000));
        Assert.False(ColorMath.PrefersDarkText(0xFF000000));
    }

    [Fact]
    public void PaletteForBackground_UsesTheColourAsTint()
    {
        var palette = WidgetPalette.ForBackground(0xB8FF9500);

        Assert.Equal(0xB8FF9500u, palette.Tint);
    }

    [Fact]
    public void PaletteForBackground_PicksTextThatReads()
    {
        Assert.Equal(WidgetPalette.Light.Text, WidgetPalette.ForBackground(0xFFFFCC00).Text);
        Assert.Equal(WidgetPalette.Dark.Text, WidgetPalette.ForBackground(0xFF5856D6).Text);
    }

    [Theory]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0xFFFFCC00u)]
    [InlineData(0xFF34C759u)]
    [InlineData(0xFF007AFFu)]
    [InlineData(0xFF5856D6u)]
    [InlineData(0xFFFF3B30u)]
    [InlineData(0xFF000000u)]
    [InlineData(0xFF808080u)]
    public void PaletteForBackground_TextMeetsLargeTextContrastOnOpaqueColours(uint argb)
    {
        var palette = WidgetPalette.ForBackground(argb);
        var (_, br, bg, bb) = ColorMath.Split(argb);
        var (_, tr, tg, tb) = ColorMath.Split(palette.Text);

        var ratio = ColorMath.ContrastRatio(ColorMath.RelativeLuminance(br, bg, bb), ColorMath.RelativeLuminance(tr, tg, tb));

        Assert.True(ratio >= 3, $"Contrast {ratio:F2} on #{argb:X8}");
    }
}
