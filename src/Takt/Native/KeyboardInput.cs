using System.Runtime.InteropServices;

namespace Takt.Native;

internal static class KeyboardInput
{
    public const ushort VirtualKeyShift = 0x10;
    public const ushort VirtualKeyControl = 0x11;
    public const ushort VirtualKeyF15 = 0x7E;

    public static bool Type(char value) => Send(value, up: false) && Send(value, up: true);

    public static bool TapVirtualKey(ushort virtualKey) =>
        Key(virtualKey, up: false) && Key(virtualKey, up: true);

    private static bool Key(ushort virtualKey, bool up)
    {
        var input = new Win32.INPUT
        {
            Type = Win32.InputKeyboard,
            Union = new Win32.InputUnion
            {
                Keyboard = new Win32.KEYBDINPUT
                {
                    WVk = virtualKey,
                    DwFlags = up ? Win32.KeyEventKeyUp : 0
                }
            }
        };
        return Win32.SendInput(1, ref input, Marshal.SizeOf<Win32.INPUT>()) != 0;
    }

    private static bool Send(char value, bool up)
    {
        var input = new Win32.INPUT
        {
            Type = Win32.InputKeyboard,
            Union = new Win32.InputUnion
            {
                Keyboard = new Win32.KEYBDINPUT
                {
                    WScan = value,
                    DwFlags = Win32.KeyEventUnicode | (up ? Win32.KeyEventKeyUp : 0)
                }
            }
        };
        return Win32.SendInput(1, ref input, Marshal.SizeOf<Win32.INPUT>()) != 0;
    }
}
