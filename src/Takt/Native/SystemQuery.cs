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

    public static bool TryGetIdleTime(out TimeSpan idle)
    {
        var info = new Win32.LASTINPUTINFO
        {
            CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Win32.LASTINPUTINFO>()
        };
        if (!Win32.GetLastInputInfo(ref info))
        {
            idle = TimeSpan.MaxValue;
            return false;
        }

        var ticks = unchecked(Win32.GetTickCount() - info.DwTime);
        idle = TimeSpan.FromMilliseconds(ticks);
        return true;
    }

    public static int? PolicyLockTimeoutSeconds()
    {
        var machine = ReadDword(
            Microsoft.Win32.Registry.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "InactivityTimeoutSecs");
        if (machine is > 0)
            return machine;

        foreach (var root in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
        {
            var timeout = ReadDword(root, @"Software\Policies\Microsoft\Windows\Control Panel\Desktop", "ScreenSaveTimeOut");
            var active = ReadString(root, @"Software\Policies\Microsoft\Windows\Control Panel\Desktop", "ScreenSaveActive");
            if (timeout is > 0 && active == "1")
                return timeout;
        }

        return null;
    }

    private static int? ReadDword(Microsoft.Win32.RegistryKey root, string path, string name)
    {
        try
        {
            using var key = root.OpenSubKey(path, writable: false);
            return key?.GetValue(name) switch
            {
                int number => number,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadString(Microsoft.Win32.RegistryKey root, string path, string name)
    {
        try
        {
            using var key = root.OpenSubKey(path, writable: false);
            return key?.GetValue(name) as string;
        }
        catch
        {
            return null;
        }
    }
}
