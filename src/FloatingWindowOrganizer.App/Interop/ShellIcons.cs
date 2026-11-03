using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloatingWindowOrganizer.App.Interop;

/// <summary>Gets the same 48x48 icons File Explorer shows.</summary>
internal static class ShellIcons
{
    private static IImageList? s_imageList;

    public static ImageSource? GetIcon(string path)
    {
        try
        {
            var info = new NativeMethods.SHFILEINFO();
            var result = NativeMethods.SHGetFileInfo(
                path, 0, ref info, (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(), NativeMethods.ShgfiSysIconIndex);
            if (result == IntPtr.Zero)
            {
                return null;
            }

            s_imageList ??= GetExtraLargeImageList();
            if (s_imageList is null
                || s_imageList.GetIcon(info.iIcon, NativeMethods.IldTransparent, out var hIcon) != 0
                || hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var image = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                image.Freeze();
                return image;
            }
            finally
            {
                NativeMethods.DestroyIcon(hIcon);
            }
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static IImageList? GetExtraLargeImageList()
    {
        var iid = typeof(IImageList).GUID;
        return NativeMethods.SHGetImageList(NativeMethods.ShilExtraLarge, ref iid, out var list) == 0 ? list : null;
    }
}
