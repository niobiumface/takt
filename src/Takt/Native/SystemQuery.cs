namespace Takt.Native;

internal static class SystemQuery
{
    public static bool IsForegroundFullscreen()
    {
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd == nint.Zero)
            return false;

        var desktop = Win32.GetDesktopWindow();
        var shell = Win32.GetShellWindow();
        if (hwnd == desktop || hwnd == shell)
            return false;

        if (!Win32.GetWindowRect(hwnd, out var app))
            return false;

        var monitor = Win32.MonitorFromWindow(hwnd, Win32.MonitorDefaultToNearest);
        var info = new Win32.MONITORINFO { CbSize = System.Runtime.InteropServices.Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(monitor, ref info))
            return false;

        var m = info.RcMonitor;
        return app.Left <= m.Left && app.Top <= m.Top && app.Right >= m.Right && app.Bottom >= m.Bottom;
    }

    public static TimeSpan GetIdleTime()
    {
        var info = new Win32.LASTINPUTINFO
        {
            CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32.LASTINPUTINFO>()
        };
        if (!Win32.GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        var idle = unchecked(Win32.GetTickCount() - info.DwTime);
        return TimeSpan.FromMilliseconds(idle);
    }
}
