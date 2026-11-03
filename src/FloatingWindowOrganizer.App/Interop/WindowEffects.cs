using System.Runtime.InteropServices;

namespace FloatingWindowOrganizer.App.Interop;

internal static class WindowEffects
{
    /// <summary>Blurs whatever is behind the window (the wallpaper, for a desktop widget).</summary>
    public static void EnableBlurBehind(IntPtr hwnd)
    {
        var accent = new NativeMethods.AccentPolicy { AccentState = NativeMethods.AccentEnableBlurBehind };
        var size = Marshal.SizeOf<NativeMethods.AccentPolicy>();
        var accentPtr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, fDeleteOld: false);
            var data = new NativeMethods.WindowCompositionAttributeData
            {
                Attribute = NativeMethods.WcaAccentPolicy,
                Data = accentPtr,
                SizeOfData = size,
            };
            NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    /// <summary>
    /// Clips the window (and therefore the blur) to a rounded rectangle. Sizes are in physical pixels.
    /// </summary>
    public static void SetRoundedRegion(IntPtr hwnd, int width, int height, int cornerDiameter)
    {
        // CreateRoundRectRgn excludes the right and bottom edges, hence the +1.
        var region = NativeMethods.CreateRoundRectRgn(0, 0, width + 1, height + 1, cornerDiameter, cornerDiameter);
        if (region == IntPtr.Zero)
        {
            return;
        }

        // On success the system owns the region; only delete it if it wasn't accepted.
        if (NativeMethods.SetWindowRgn(hwnd, region, bRedraw: true) == 0)
        {
            NativeMethods.DeleteObject(region);
        }
    }
}
