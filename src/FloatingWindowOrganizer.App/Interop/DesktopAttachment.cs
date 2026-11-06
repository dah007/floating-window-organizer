namespace FloatingWindowOrganizer.App.Interop;

/// <summary>
/// Pins a window to the desktop layer: just above the wallpaper, below normal app windows.
/// </summary>
internal static class DesktopAttachment
{
    public static void Attach(IntPtr hwnd)
    {
        // Tool window: hidden from Alt+Tab. No-activate: clicking it never brings it in front of other apps.
        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        exStyle |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new IntPtr(exStyle));

        // Make the desktop (Progman) the window's owner. Owned windows always sit just above their
        // owner, so when Win+D / Show desktop brings the desktop forward, the widget comes with it.
        var desktop = NativeMethods.FindWindow("Progman", null);
        if (desktop != IntPtr.Zero)
        {
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlpHwndParent, desktop);
        }

        SendToBottom(hwnd);
    }

    /// <summary>
    /// Lets the window take keyboard focus (for renaming) or stops it again. Widgets are
    /// normally no-activate so clicking them never pulls them in front of other apps.
    /// </summary>
    public static void SetActivatable(IntPtr hwnd, bool activatable)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        exStyle = activatable ? exStyle & ~NativeMethods.WsExNoActivate : exStyle | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new IntPtr(exStyle));
    }

    public static void SendToBottom(IntPtr hwnd) =>
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndBottom,
            0, 0, 0, 0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
}
