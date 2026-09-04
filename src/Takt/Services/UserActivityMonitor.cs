using System.Diagnostics;
using Takt.Helpers;
using Takt.Native;

namespace Takt.Services;

internal sealed class UserActivityMonitor : IDisposable
{
    private readonly Win32.HookProc _mouseProc;
    private readonly Win32.HookProc _keyboardProc;
    private nint _mouseHook;
    private nint _keyboardHook;
    private Win32.POINT _anchor;
    private bool _hasAnchor;

    public UserActivityMonitor()
    {
        _mouseProc = OnMouse;
        _keyboardProc = OnKeyboard;
        LastPhysicalUtc = DateTime.UtcNow.AddMinutes(-5);
    }

    public DateTime LastPhysicalUtc { get; private set; }

    public TimeSpan IdleTime => DateTime.UtcNow - LastPhysicalUtc;

    public void Start()
    {
        if (_mouseHook != nint.Zero)
            return;

        var module = Win32.GetModuleHandle(null);
        if (module == nint.Zero)
            module = Win32.GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);

        _mouseHook = Win32.SetWindowsHookEx(Win32.WhMouseLl, _mouseProc, module, 0);
        _keyboardHook = Win32.SetWindowsHookEx(Win32.WhKeyboardLl, _keyboardProc, module, 0);
    }

    public void Dispose()
    {
        if (_mouseHook != nint.Zero)
        {
            Win32.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
        }

        if (_keyboardHook != nint.Zero)
        {
            Win32.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = nint.Zero;
        }
    }

    private nint OnMouse(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= Win32.HcAction)
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32.MSLLHOOKSTRUCT>(lParam);
            if (!UserActivityFilter.IsInjected(data.Flags, keyboard: false))
                ConsiderMouse((int)wParam, data.Pt);
        }

        return Win32.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private nint OnKeyboard(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= Win32.HcAction)
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);
            if (!UserActivityFilter.IsInjected(data.Flags, keyboard: true))
                LastPhysicalUtc = DateTime.UtcNow;
        }

        return Win32.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void ConsiderMouse(int message, Win32.POINT point)
    {
        if (UserActivityFilter.IsButtonOrWheel(message))
        {
            LastPhysicalUtc = DateTime.UtcNow;
            _anchor = point;
            _hasAnchor = true;
            return;
        }

        if (!UserActivityFilter.IsMouseMove(message))
            return;

        if (!_hasAnchor)
        {
            _anchor = point;
            _hasAnchor = true;
            return;
        }

        if (!UserActivityFilter.IsSignificantMove(_anchor.X, _anchor.Y, point.X, point.Y))
            return;

        _anchor = point;
        LastPhysicalUtc = DateTime.UtcNow;
    }
}
