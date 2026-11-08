using System.Windows;
using System.Windows.Media;
using FloatingWindowOrganizer.App;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.AppTests;

public sealed class WidgetThemeTests
{
    [Fact]
    public void Apply_SetsAllSixBrushesFromThePalette()
    {
        var resources = new ResourceDictionary();

        WidgetTheme.Apply(resources, WidgetPalette.Dark);

        AssertBrush(resources, WidgetTheme.TintBrushKey, WidgetPalette.Dark.Tint);
        AssertBrush(resources, WidgetTheme.BorderBrushKey, WidgetPalette.Dark.Border);
        AssertBrush(resources, WidgetTheme.TextBrushKey, WidgetPalette.Dark.Text);
        AssertBrush(resources, WidgetTheme.SecondaryTextBrushKey, WidgetPalette.Dark.SecondaryText);
        AssertBrush(resources, WidgetTheme.TileBrushKey, WidgetPalette.Dark.Tile);
        AssertBrush(resources, WidgetTheme.TileHoverBrushKey, WidgetPalette.Dark.TileHover);
    }

    [Fact]
    public void Clear_RemovesOnlyTheWidgetBrushKeys()
    {
        var resources = new ResourceDictionary { ["SomeOtherAppResource"] = "keep me" };
        WidgetTheme.Apply(resources, WidgetPalette.Light);

        WidgetTheme.Clear(resources);

        Assert.False(resources.Contains(WidgetTheme.TintBrushKey));
        Assert.False(resources.Contains(WidgetTheme.BorderBrushKey));
        Assert.False(resources.Contains(WidgetTheme.TextBrushKey));
        Assert.False(resources.Contains(WidgetTheme.SecondaryTextBrushKey));
        Assert.False(resources.Contains(WidgetTheme.TileBrushKey));
        Assert.False(resources.Contains(WidgetTheme.TileHoverBrushKey));
        Assert.Equal("keep me", resources["SomeOtherAppResource"]);
    }

    [Theory]
    [InlineData(0xFF000000u)]
    [InlineData(0x00FFFFFFu)]
    [InlineData(0xB8007AFFu)]
    public void ToBrush_RoundTripsArgbAndIsFrozen(uint argb)
    {
        var brush = WidgetTheme.ToBrush(argb);
        var (a, r, g, b) = ColorMath.Split(argb);

        Assert.Equal(Color.FromArgb(a, r, g, b), brush.Color);
        Assert.True(brush.IsFrozen);
    }

    private static void AssertBrush(ResourceDictionary resources, string key, uint expectedArgb)
    {
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);
        var (a, r, g, b) = ColorMath.Split(expectedArgb);
        Assert.Equal(Color.FromArgb(a, r, g, b), brush.Color);
    }
}
