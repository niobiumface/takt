using System.Runtime.InteropServices;
using Takt.Helpers;
using Takt.Models;

namespace Takt.Native;

internal static class MouseInput
{
    public static bool TryGetCursorPos(out Win32.POINT point) => Win32.GetCursorPos(out point);

    public static Win32.POINT GetCursorPos()
    {
        TryGetCursorPos(out var point);
        return point;
    }

    public static bool TryGetMonitor(int x, int y, out PixelRect monitor)
    {
        var handle = Win32.MonitorFromPoint(new Win32.POINT { X = x, Y = y }, Win32.MonitorDefaultToNull);
        if (handle == nint.Zero)
        {
            monitor = default;
            return false;
        }

        var info = new Win32.MONITORINFO { CbSize = Marshal.SizeOf<Win32.MONITORINFO>() };
        if (!Win32.GetMonitorInfo(handle, ref info))
        {
            monitor = default;
            return false;
        }

        var rect = info.RcMonitor;
        monitor = new PixelRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return monitor.Right > monitor.Left && monitor.Bottom > monitor.Top;
    }

    public static VirtualScreen GetVirtualScreen() => new(
        Win32.GetSystemMetrics(Win32.SmXVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmYVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmCxVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmCyVirtualScreen));

    public static bool MoveRelative(int dx, int dy) =>
        SendFlags(dx, dy, Win32.MouseEventMove);

    public static bool MoveAbsolute(int pixelX, int pixelY)
    {
        var screen = GetVirtualScreen();
        var mapped = CoordinateMapper.ToAbsolute65535(pixelX, pixelY, screen);
        return SendFlags(mapped.X, mapped.Y, Win32.MouseEventMove | Win32.MouseEventAbsolute | Win32.MouseEventVirtualDesk);
    }

    public static bool Nudge(int amplitude)
    {
        if (!TryGetCursorPos(out var pos))
            return false;

        var amp = Math.Max(amplitude, 1);
        var outOk = MoveAbsolute(pos.X + amp, pos.Y);
        Thread.Sleep(40);
        var backOk = MoveAbsolute(pos.X, pos.Y);
        return outOk && backOk;
    }

    public static bool Click(ClickButton button, ClickAction action)
    {
        if (PointerButtons.IsWheel(button))
        {
            var (_, data) = PointerButtons.Describe(button, down: true);
            var once = Wheel(unchecked((int)data));
            if (action != ClickAction.Double)
                return once;
            Thread.Sleep(40);
            return once && Wheel(unchecked((int)data));
        }

        switch (action)
        {
            case ClickAction.Hold:
                return Down(button);
            case ClickAction.Double:
                if (!Pulse(button))
                    return false;
                Thread.Sleep(40);
                return Pulse(button);
            default:
                return Pulse(button);
        }
    }

    public static bool Down(ClickButton button)
    {
        var (flags, data) = PointerButtons.Describe(button, down: true);
        return SendFlags(0, 0, flags, data);
    }

    public static bool Up(ClickButton button)
    {
        var (flags, data) = PointerButtons.Describe(button, down: false);
        return SendFlags(0, 0, flags, data);
    }

    public static bool Wheel(int delta) =>
        SendFlags(0, 0, PointerButtons.Wheel, unchecked((uint)delta));

    private static bool Pulse(ClickButton button)
    {
        if (!Down(button))
            return false;
        Thread.Sleep(8);
        return Up(button);
    }

    private static bool SendFlags(int dx, int dy, uint flags, uint mouseData = 0)
    {
        if ((flags & Win32.MouseEventMove) != 0)
        {
            if (TrySend(dx, dy, flags | Win32.MouseEventMoveNoCoalesce, mouseData))
                return true;
        }

        return TrySend(dx, dy, flags, mouseData);
    }

    private static bool TrySend(int dx, int dy, uint flags, uint mouseData)
    {
        var input = new Win32.INPUT
        {
            Type = Win32.InputMouse,
            Union = new Win32.InputUnion
            {
                Mouse = new Win32.MOUSEINPUT
                {
                    Dx = dx,
                    Dy = dy,
                    MouseData = mouseData,
                    DwFlags = flags
                }
            }
        };
        var size = Marshal.SizeOf<Win32.INPUT>();
        return Win32.SendInput(1, ref input, size) != 0;
    }
}
