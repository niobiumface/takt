using System.Text;

namespace Takt.Native;

internal static class ForegroundProcess
{
    public static string? TryGetName() => TryGetPath(Win32.GetForegroundWindow());

    public static string? TryGetPath(nint hwnd)
    {
        try
        {
            if (hwnd == nint.Zero)
                return null;
            Win32.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
                return null;
            var handle = Win32.OpenProcess(Win32.ProcessQueryLimitedInformation, false, pid);
            if (handle == nint.Zero)
                return null;
            try
            {
                var buffer = new StringBuilder(1024);
                var size = (uint)buffer.Capacity;
                if (!Win32.QueryFullProcessImageName(handle, 0, buffer, ref size))
                    return null;
                return buffer.ToString();
            }
            finally
            {
                Win32.CloseHandle(handle);
            }
        }
        catch
        {
            return null;
        }
    }
}
