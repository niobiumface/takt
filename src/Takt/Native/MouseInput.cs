using System.Runtime.InteropServices;
using Takt.Helpers;
using Takt.Models;

namespace Takt.Native;

internal static class MouseInput
{
    public static Win32.POINT GetCursorPos()
    {
        Win32.GetCursorPos(out var point);
        return point;
    }

    public static VirtualScreen GetVirtualScreen() => new(
        Win32.GetSystemMetrics(Win32.SmXVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmYVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmCxVirtualScreen),
        Win32.GetSystemMetrics(Win32.SmCyVirtualScreen));

    public static void MoveRelative(int dx, int dy)
    {
        SendFlags(dx, dy, Win32.MouseEventMove);
    }

    public static void MoveAbsolute(int pixelX, int pixelY)
    {
        var screen = GetVirtualScreen();
        var mapped = CoordinateMapper.ToAbsolute65535(pixelX, pixelY, screen);
        SendFlags(mapped.X, mapped.Y, Win32.MouseEventMove | Win32.MouseEventAbsolute | Win32.MouseEventVirtualDesk);
    }

    public static void Nudge(int amplitude)
    {
        var pos = GetCursorPos();
        var amp = Math.Max(amplitude, 1);
        MoveAbsolute(pos.X + amp, pos.Y);
        Thread.Sleep(40);
        MoveAbsolute(pos.X, pos.Y);
    }

    public static void Click(ClickButton button, ClickAction action)
    {
        switch (action)
        {
            case ClickAction.Hold:
                Down(button);
                break;
            case ClickAction.Double:
                Pulse(button);
                Thread.Sleep(40);
                Pulse(button);
                break;
            default:
                Pulse(button);
                break;
        }
    }

    public static void Down(ClickButton button) => SendFlags(0, 0, DownFlag(button));

    public static void Up(ClickButton button) => SendFlags(0, 0, UpFlag(button));

    private static void Pulse(ClickButton button)
    {
        Down(button);
        Thread.Sleep(8);
        Up(button);
    }

    private static uint DownFlag(ClickButton button) => button switch
    {
        ClickButton.Right => Win32.MouseEventRightDown,
        ClickButton.Middle => Win32.MouseEventMiddleDown,
        _ => Win32.MouseEventLeftDown
    };

    private static uint UpFlag(ClickButton button) => button switch
    {
        ClickButton.Right => Win32.MouseEventRightUp,
        ClickButton.Middle => Win32.MouseEventMiddleUp,
        _ => Win32.MouseEventLeftUp
    };

    private static void SendFlags(int dx, int dy, uint flags)
    {
        if ((flags & Win32.MouseEventMove) != 0)
        {
            if (TrySend(dx, dy, flags | Win32.MouseEventMoveNoCoalesce))
                return;
        }

        TrySend(dx, dy, flags);
    }

    private static bool TrySend(int dx, int dy, uint flags)
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
                    DwFlags = flags
                }
            }
        };
        var size = Marshal.SizeOf<Win32.INPUT>();
        return Win32.SendInput(1, ref input, size) != 0;
    }
}
