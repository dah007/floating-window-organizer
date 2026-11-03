using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class WindowsThemeTests
{
    [Fact]
    public void RegistryValueZero_MeansDark()
    {
        Assert.False(WindowsTheme.IsLightTheme(0));
    }

    [Fact]
    public void RegistryValueOne_MeansLight()
    {
        Assert.True(WindowsTheme.IsLightTheme(1));
    }

    [Fact]
    public void MissingValue_DefaultsToLight()
    {
        Assert.True(WindowsTheme.IsLightTheme(null));
    }

    [Fact]
    public void UnexpectedValueType_DefaultsToLight()
    {
        Assert.True(WindowsTheme.IsLightTheme("unexpected"));
    }

    [Fact]
    public void PaletteFor_PicksMatchingPalette()
    {
        Assert.Same(WidgetPalette.Light, WindowsTheme.PaletteFor(isLightTheme: true));
        Assert.Same(WidgetPalette.Dark, WindowsTheme.PaletteFor(isLightTheme: false));
    }

    public static TheoryData<string> Palettes => new() { "Light", "Dark" };

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Palette_TintIsTranslucentSoTheBlurShowsThrough(string name)
    {
        var palette = name == "Light" ? WidgetPalette.Light : WidgetPalette.Dark;
        var alpha = palette.Tint >> 24;

        Assert.InRange(alpha, 0x40u, 0xE0u);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void Palette_TextIsFullyOpaque(string name)
    {
        var palette = name == "Light" ? WidgetPalette.Light : WidgetPalette.Dark;

        Assert.Equal(0xFFu, palette.Text >> 24);
        Assert.Equal(0xFFu, palette.SecondaryText >> 24);
    }
}
