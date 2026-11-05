using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Takt.Services;

internal sealed class SessionWatch
{
    public const int WmWtsSessionChange = 0x02B1;
    public const int WtsSessionLock = 0x7;
    public const int WtsSessionUnlock = 0x8;

    private HwndSource? _source;

    public event Action<bool>? LockChanged;

    public void Attach(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero)
            return;
        WTSRegisterSessionNotification(hwnd, 0);
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
    }

    public void Detach()
    {
        if (_source is null)
            return;
        WTSUnRegisterSessionNotification(_source.Handle);
        _source.RemoveHook(WndProc);
        _source = null;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmWtsSessionChange)
        {
            var code = wParam.ToInt32();
            if (code == WtsSessionLock)
                LockChanged?.Invoke(true);
            else if (code == WtsSessionUnlock)
                LockChanged?.Invoke(false);
        }

        return nint.Zero;
    }

    [DllImport("wtsapi32.dll")]
    private static extern bool WTSRegisterSessionNotification(nint hwnd, int flags);

    [DllImport("wtsapi32.dll")]
    private static extern bool WTSUnRegisterSessionNotification(nint hwnd);
}
