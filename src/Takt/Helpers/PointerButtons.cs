using Takt.Models;

namespace Takt.Helpers;

public static class PointerButtons
{
    public const uint LeftDown = 0x0002;
    public const uint LeftUp = 0x0004;
    public const uint RightDown = 0x0008;
    public const uint RightUp = 0x0010;
    public const uint MiddleDown = 0x0020;
    public const uint MiddleUp = 0x0040;
    public const uint XDown = 0x0080;
    public const uint XUp = 0x0100;
    public const uint Wheel = 0x0800;
    public const int WheelDelta = 120;

    public static bool IsWheel(ClickButton button) =>
        button is ClickButton.WheelUp or ClickButton.WheelDown;

    public static (uint Flags, uint Data) Describe(ClickButton button, bool down)
    {
        return button switch
        {
            ClickButton.Right => (down ? RightDown : RightUp, 0),
            ClickButton.Middle => (down ? MiddleDown : MiddleUp, 0),
            ClickButton.X1 => (down ? XDown : XUp, 1u << 16),
            ClickButton.X2 => (down ? XDown : XUp, 2u << 16),
            ClickButton.WheelUp => (Wheel, WheelDelta),
            ClickButton.WheelDown => (Wheel, unchecked((uint)-WheelDelta)),
            _ => (down ? LeftDown : LeftUp, 0)
        };
    }
}
