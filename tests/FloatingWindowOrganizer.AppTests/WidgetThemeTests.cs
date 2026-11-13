using System.Windows;
using System.Windows.Media;
using FloatingWindowOrganizer.App;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.AppTests;

public sealed class WidgetThemeTests
{
    [Fact]
    public void Apply_SetsAllBrushesFromThePalette()
    {
        var resources = new ResourceDictionary();

        WidgetTheme.Apply(resources, WidgetPalette.Dark);

        AssertBrush(resources, WidgetTheme.TintBrushKey, WidgetPalette.Dark.Tint);
        AssertBrush(resources, WidgetTheme.BorderBrushKey, WidgetPalette.Dark.Border);
        AssertBrush(resources, WidgetTheme.TextBrushKey, WidgetPalette.Dark.Text);
        AssertBrush(resources, WidgetTheme.SecondaryTextBrushKey, WidgetPalette.Dark.SecondaryText);
        AssertBrush(resources, WidgetTheme.TileBrushKey, WidgetPalette.Dark.Tile);
        AssertBrush(resources, WidgetTheme.TileHoverBrushKey, WidgetPalette.Dark.TileHover);
        AssertBrush(resources, WidgetTheme.GlossVeilBrushKey, WidgetPalette.Dark.GlossVeil);
        Assert.IsType<LinearGradientBrush>(resources[WidgetTheme.GlossBrushKey]);
        Assert.IsType<LinearGradientBrush>(resources[WidgetTheme.GlossStreakBrushKey]);
        Assert.IsType<LinearGradientBrush>(resources[WidgetTheme.GlossRimBrushKey]);
        Assert.IsType<LinearGradientBrush>(resources[WidgetTheme.TileGlossBrushKey]);
        AssertBrush(resources, WidgetTheme.TileEdgeBrushKey, WidgetPalette.Dark.TileEdge);
    }

    [Fact]
    public void Apply_GlossFollowsThePalette_LightAndDarkDiffer()
    {
        var light = new ResourceDictionary();
        var dark = new ResourceDictionary();

        WidgetTheme.Apply(light, WidgetPalette.Light);
        WidgetTheme.Apply(dark, WidgetPalette.Dark);

        Assert.Equal(PeakColor(WidgetPalette.Light.Gloss), FirstStop(light, WidgetTheme.GlossBrushKey));
        Assert.Equal(PeakColor(WidgetPalette.Dark.Gloss), FirstStop(dark, WidgetTheme.GlossBrushKey));
        Assert.Equal(PeakColor(WidgetPalette.Light.GlossRim), FirstStop(light, WidgetTheme.GlossRimBrushKey));
        Assert.Equal(PeakColor(WidgetPalette.Dark.GlossRim), FirstStop(dark, WidgetTheme.GlossRimBrushKey));
        Assert.NotEqual(
            ((SolidColorBrush)light[WidgetTheme.GlossVeilBrushKey]).Color,
            ((SolidColorBrush)dark[WidgetTheme.GlossVeilBrushKey]).Color);
    }

    [Fact]
    public void Apply_CustomColorPalette_CarriesTheMatchingGloss()
    {
        var resources = new ResourceDictionary();

        WidgetTheme.Apply(resources, WidgetPalette.ForBackground(0xFF101010));
        Assert.Equal(PeakColor(WidgetPalette.Dark.Gloss), FirstStop(resources, WidgetTheme.GlossBrushKey));

        WidgetTheme.Apply(resources, WidgetPalette.ForBackground(0xFFF0F0F0));
        Assert.Equal(PeakColor(WidgetPalette.Light.Gloss), FirstStop(resources, WidgetTheme.GlossBrushKey));
    }

    [Fact]
    public void CreateGlossBrush_GlareAtTheTopGoneBeforeTheMiddle_BounceAtTheBottom()
    {
        var brush = WidgetTheme.CreateGlossBrush(0x59FFFFFF, 0x15FFFFFF);

        Assert.True(brush.IsFrozen);
        Assert.Equal(new Point(0, 0), brush.StartPoint);
        Assert.Equal(new Point(0, 1), brush.EndPoint);
        var stops = brush.GradientStops.OrderBy(s => s.Offset).ToList();
        Assert.Equal(Color.FromArgb(0x59, 0xFF, 0xFF, 0xFF), stops[0].Color);
        Assert.All(stops.Where(s => s.Offset >= 0.46 && s.Offset < 1), s => Assert.Equal(0, s.Color.A)); // clear middle
        Assert.Equal(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF), stops[^1].Color);
        Assert.All(stops, s => Assert.Equal(0xFF, s.Color.R));
    }

    [Fact]
    public void CreateGlossStreakBrush_IsADiagonalBandThatFadesOutOnBothSides()
    {
        var brush = WidgetTheme.CreateGlossStreakBrush(0x19FFFFFF);

        Assert.True(brush.IsFrozen);
        Assert.True(brush.EndPoint.X > brush.StartPoint.X && brush.EndPoint.Y > brush.StartPoint.Y);
        var stops = brush.GradientStops.OrderBy(s => s.Offset).ToList();
        Assert.Equal(0, stops[0].Color.A);
        Assert.Equal(0, stops[^1].Color.A);
        Assert.Equal(0x19, stops.Max(s => s.Color.A));
    }

    [Fact]
    public void CreateGlossRimBrush_IsLightAlongTheTopAndDarkAlongTheBottom()
    {
        var brush = WidgetTheme.CreateGlossRimBrush(0xE0FFFFFF, 0x26000000);

        Assert.True(brush.IsFrozen);
        Assert.Equal(new Point(0, 0), brush.StartPoint);
        Assert.Equal(new Point(0, 1), brush.EndPoint);
        var stops = brush.GradientStops.OrderBy(s => s.Offset).ToList();
        Assert.Equal(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF), stops[0].Color);
        Assert.Equal(Color.FromArgb(0x26, 0, 0, 0), stops[^1].Color);
    }

    [Fact]
    public void CreateTileGlossBrush_FadesOutJustPastTheMiddle()
    {
        var brush = WidgetTheme.CreateTileGlossBrush(0x59FFFFFF);

        var stops = brush.GradientStops.OrderBy(s => s.Offset).ToList();
        Assert.True(brush.IsFrozen);
        Assert.Equal(Color.FromArgb(0x59, 0xFF, 0xFF, 0xFF), stops[0].Color);
        Assert.Equal(0, stops[^1].Color.A);
        Assert.InRange(stops[^1].Offset, 0.5, 0.7);
    }

    [Fact]
    public void Clear_AlsoRemovesTheGlossBrushes()
    {
        var resources = new ResourceDictionary();
        WidgetTheme.Apply(resources, WidgetPalette.Light);

        WidgetTheme.Clear(resources);

        Assert.False(resources.Contains(WidgetTheme.GlossVeilBrushKey));
        Assert.False(resources.Contains(WidgetTheme.GlossBrushKey));
        Assert.False(resources.Contains(WidgetTheme.GlossStreakBrushKey));
        Assert.False(resources.Contains(WidgetTheme.GlossRimBrushKey));
        Assert.False(resources.Contains(WidgetTheme.TileGlossBrushKey));
        Assert.False(resources.Contains(WidgetTheme.TileEdgeBrushKey));
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

    private static Color PeakColor(uint argb)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        return Color.FromArgb(a, r, g, b);
    }

    /// <summary>The colour of the first (offset 0) stop of a gloss gradient.</summary>
    private static Color FirstStop(ResourceDictionary resources, string key) =>
        Assert.IsAssignableFrom<GradientBrush>(resources[key]).GradientStops.OrderBy(s => s.Offset).First().Color;

    private static void AssertBrush(ResourceDictionary resources, string key, uint expectedArgb)
    {
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);
        var (a, r, g, b) = ColorMath.Split(expectedArgb);
        Assert.Equal(Color.FromArgb(a, r, g, b), brush.Color);
    }
}
