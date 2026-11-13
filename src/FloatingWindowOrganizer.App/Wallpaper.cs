using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;
using Microsoft.Win32;

namespace FloatingWindowOrganizer.App;

/// <summary>Reads the desktop wallpaper's average colour, which custom-coloured glass is judged against.</summary>
internal static class Wallpaper
{
    private const uint SpiGetDeskWallpaper = 0x0073;
    private const int MaxPath = 260;

    // Decoding a few dozen pixels across is plenty for an average and keeps this fast for huge images.
    private const int SampleWidth = 32;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, StringBuilder value, uint winIni);

    /// <summary>
    /// The wallpaper image's average colour; the solid desktop colour when there is no image; null when
    /// neither can be read (callers then assume mid-grey).
    /// </summary>
    public static uint? AverageColor()
    {
        try
        {
            var path = ImagePath();
            return path is not null && File.Exists(path) ? AverageOf(path) : SolidColor();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
                                       or FileFormatException or InvalidOperationException or ArgumentException
                                       or COMException)
        {
            return SolidColor();
        }
    }

    private static string? ImagePath()
    {
        var buffer = new StringBuilder(MaxPath);
        return SystemParametersInfo(SpiGetDeskWallpaper, MaxPath, buffer, 0) && buffer.Length > 0
            ? buffer.ToString()
            : null;
    }

    private static uint AverageOf(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path);
        image.DecodePixelWidth = SampleWidth;
        image.CacheOption = BitmapCacheOption.OnLoad; // don't keep the wallpaper file locked
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.EndInit();

        var pixels = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var stride = pixels.PixelWidth * 4;
        var data = new byte[stride * pixels.PixelHeight];
        pixels.CopyPixels(data, stride, 0);

        long b = 0, g = 0, r = 0;
        var count = data.Length / 4;
        for (var i = 0; i < data.Length; i += 4)
        {
            b += data[i];
            g += data[i + 1];
            r += data[i + 2];
        }

        return count == 0
            ? WidgetPalette.DefaultWallpaper
            : ColorMath.ToArgb(0xFF, (byte)(r / count), (byte)(g / count), (byte)(b / count));
    }

    /// <summary>The solid desktop colour ("R G B" under Control Panel\Colors), or null.</summary>
    private static uint? SolidColor()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors");
        if (key?.GetValue("Background") is not string text)
        {
            return null;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3
               && byte.TryParse(parts[0], out var r) && byte.TryParse(parts[1], out var g) && byte.TryParse(parts[2], out var b)
            ? ColorMath.ToArgb(0xFF, r, g, b)
            : null;
    }
}
