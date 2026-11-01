using System.Windows;
using System.Windows.Interop;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal enum HotkeyAction
{
    ToggleJiggler = 1,
    ToggleClicker = 2,
    ToggleBoth = 3,
    Panic = 4,
    PickPosition = 5,
    ToggleMacro = 6
}

internal sealed class HotkeyService : IDisposable
{
    public static readonly uint ShowMessage = Win32.RegisterWindowMessage("Takt.ShowWindow");

    private readonly HotkeySettings _settings;
    private Window? _window;
    private HwndSource? _source;
    private nint _hwnd;
    private bool _suspended;
    private readonly HashSet<int> _registered = [];

    public HotkeyService(HotkeySettings settings) => _settings = settings;

    public event Action<HotkeyAction>? Pressed;
    public event Action? ShowRequested;
    public event Action<string>? RegistrationFailed;

    public void Attach(Window window)
    {
        _window = window;
        if (window.IsLoaded)
            Hook();
        else
            window.SourceInitialized += (_, _) => Hook();
    }

    public void Suspend()
    {
        _suspended = true;
        UnregisterAll();
    }

    public void Resume()
    {
        _suspended = false;
        if (_hwnd == nint.Zero)
            return;
        UnregisterAll();
        TryRegisterAll();
    }

    public void Rebind()
    {
        if (_hwnd == nint.Zero || _suspended)
            return;
        UnregisterAll();
        TryRegisterAll();
    }

    public static void RequestShowExisting() =>
        Win32.PostMessage(Win32.HwndBroadcast, ShowMessage, nint.Zero, nint.Zero);

    public void Dispose()
    {
        UnregisterAll();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private void Hook()
    {
        if (_window is null)
            return;
        _hwnd = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
        TryRegisterAll();
    }

    private void TryRegisterAll()
    {
        var seen = new HashSet<(int Modifiers, int VirtualKey)>();
        TryRegister(HotkeyAction.ToggleJiggler, _settings.ToggleJiggler, seen);
        TryRegister(HotkeyAction.ToggleClicker, _settings.ToggleClicker, seen);
        TryRegister(HotkeyAction.ToggleBoth, _settings.ToggleBoth, seen);
        TryRegister(HotkeyAction.Panic, _settings.Panic, seen);
        TryRegister(HotkeyAction.PickPosition, _settings.PickPosition, seen);
        TryRegister(HotkeyAction.ToggleMacro, _settings.ToggleMacro, seen);
    }

    private void TryRegister(HotkeyAction action, HotkeyBinding binding, HashSet<(int Modifiers, int VirtualKey)> seen)
    {
        if (!binding.IsValid)
            return;

        if (!seen.Add((binding.Modifiers, binding.VirtualKey)))
        {
            RegistrationFailed?.Invoke(action.ToString());
            return;
        }

        var mods = (uint)binding.Modifiers | Win32.ModNoRepeat;
        if (Win32.RegisterHotKey(_hwnd, (int)action, mods, (uint)binding.VirtualKey))
        {
            _registered.Add((int)action);
            return;
        }

        RegistrationFailed?.Invoke(action.ToString());
    }

    private void UnregisterAll()
    {
        if (_hwnd == nint.Zero)
            return;
        foreach (var id in _registered)
            Win32.UnregisterHotKey(_hwnd, id);
        _registered.Clear();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if ((uint)msg == ShowMessage)
        {
            ShowRequested?.Invoke();
            handled = true;
            return nint.Zero;
        }

        if (msg == Win32.WmHotkey)
        {
            var id = wParam.ToInt32();
            if (Enum.IsDefined(typeof(HotkeyAction), id))
                Pressed?.Invoke((HotkeyAction)id);
            handled = true;
        }

        return nint.Zero;
    }
}
