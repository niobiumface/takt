using System.Runtime.InteropServices;

namespace Takt.Native;

internal static class KeyboardInput
{
    public static bool Type(char value) => Send(value, up: false) && Send(value, up: true);

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
