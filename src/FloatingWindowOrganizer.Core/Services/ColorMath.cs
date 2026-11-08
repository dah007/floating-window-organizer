using System.Globalization;

namespace FloatingWindowOrganizer.Core.Services;

/// <summary>
/// Colour conversions for the widget colour picker. Colours are 0xAARRGGBB values; hue is
/// 0–360 degrees, saturation and value (brightness) are 0–1.
/// </summary>
public static class ColorMath
{
    // The widget text colours, so "readable" is judged against what is actually drawn.
    private const uint DarkText = 0xFF1C1C1E;
    private const uint LightText = 0xFFFFFFFF;

    // A translucent colour shows the wallpaper through it; judge it as if over a mid-grey wallpaper.
    private const byte AssumedBackdrop = 0x80;

    public static (byte R, byte G, byte B) HsvToRgb(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
        var m = value - chroma;

        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (chroma, x, 0d),
            1 => (x, chroma, 0d),
            2 => (0d, chroma, x),
            3 => (0d, x, chroma),
            4 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };

        return (ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    public static (double Hue, double Saturation, double Value) RgbToHsv(byte r, byte g, byte b)
    {
        double red = r / 255.0, green = g / 255.0, blue = b / 255.0;
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        double hue;
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == red)
        {
            hue = 60 * (((green - blue) / delta) % 6);
        }
        else if (max == green)
        {
            hue = 60 * (((blue - red) / delta) + 2);
        }
        else
        {
            hue = 60 * (((red - green) / delta) + 4);
        }

        if (hue < 0)
        {
            hue += 360;
        }

        var saturation = max == 0 ? 0 : delta / max;
        return (hue, saturation, max);
    }

    public static uint ToArgb(byte a, byte r, byte g, byte b) =>
        ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

    public static (byte A, byte R, byte G, byte B) Split(uint argb) =>
        ((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    /// <summary>"#AARRGGBB", the form saved in settings.json.</summary>
    public static string ToArgbHex(uint argb) => "#" + argb.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>"#RRGGBB", the form shown in the picker's hex box.</summary>
    public static string ToRgbHex(uint argb) => "#" + (argb & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses "#RRGGBB" or "#AARRGGBB" (the # is optional, surrounding spaces are ignored).
    /// Six digits mean fully opaque; <paramref name="hasAlpha"/> says whether alpha was given.
    /// </summary>
    public static bool TryParseHex(string? text, out uint argb, out bool hasAlpha)
    {
        argb = 0;
        hasAlpha = false;

        var digits = text?.Trim();
        if (string.IsNullOrEmpty(digits))
        {
            return false;
        }

        if (digits[0] == '#')
        {
            digits = digits[1..];
        }

        if ((digits.Length != 6 && digits.Length != 8) || !digits.All(Uri.IsHexDigit)
            || !uint.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        hasAlpha = digits.Length == 8;
        argb = hasAlpha ? value : 0xFF000000 | value;
        return true;
    }

    /// <summary>WCAG relative luminance of an opaque colour.</summary>
    public static double RelativeLuminance(byte r, byte g, byte b) =>
        (0.2126 * Linear(r)) + (0.7152 * Linear(g)) + (0.0722 * Linear(b));

    /// <summary>WCAG contrast ratio between two luminances (1 to 21).</summary>
    public static double ContrastRatio(double luminanceA, double luminanceB)
    {
        var lighter = Math.Max(luminanceA, luminanceB);
        var darker = Math.Min(luminanceA, luminanceB);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// True when dark widget text reads better than white on this background. A translucent
    /// colour is judged as if over a mid-grey wallpaper.
    /// </summary>
    public static bool PrefersDarkText(uint argb)
    {
        var (a, r, g, b) = Split(argb);
        var alpha = a / 255.0;
        byte Over(byte channel) => (byte)Math.Round((channel * alpha) + (AssumedBackdrop * (1 - alpha)));

        var background = RelativeLuminance(Over(r), Over(g), Over(b));
        var (_, dr, dg, db) = Split(DarkText);
        var (_, lr, lg, lb) = Split(LightText);

        return ContrastRatio(background, RelativeLuminance(dr, dg, db))
            >= ContrastRatio(background, RelativeLuminance(lr, lg, lb));
    }

    private static byte ToByte(double unit) => (byte)Math.Round(Math.Clamp(unit, 0, 1) * 255);

    private static double Linear(byte channel)
    {
        var c = channel / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
