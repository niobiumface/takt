using Takt.Helpers;

namespace Takt.Native;

internal readonly record struct MonitorDisplay(PixelRect Pixels, uint DpiX, uint DpiY);

internal static class MonitorQuery
{
    public static IReadOnlyList<MonitorDisplay> All()
    {
        var found = new List<MonitorDisplay>();
        Win32.MonitorEnumProc callback = (monitor, _, _, _) =>
        {
            var info = new Win32.MONITORINFO { CbSize = System.Runtime.InteropServices.Marshal.SizeOf<Win32.MONITORINFO>() };
            if (!Win32.GetMonitorInfo(monitor, ref info))
                return true;

            uint dpiX = 96;
            uint dpiY = 96;
            try
            {
                Win32.GetDpiForMonitor(monitor, 0, out dpiX, out dpiY);
            }
            catch (DllNotFoundException)
            {
                dpiX = dpiY = 96;
            }
            catch (EntryPointNotFoundException)
            {
                dpiX = dpiY = 96;
            }

            if (dpiX == 0) dpiX = 96;
            if (dpiY == 0) dpiY = 96;
            var rect = info.RcMonitor;
            found.Add(new MonitorDisplay(new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom), dpiX, dpiY));
            return true;
        };

        Win32.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
        return found;
    }
}
