using CommunityToolkit.Mvvm.ComponentModel;

namespace Takt.Models;

public partial class HotkeyBinding : ObservableObject
{
    [ObservableProperty] private int _modifiers;
    [ObservableProperty] private int _virtualKey = 0x75;

    public static HotkeyBinding Vk(int virtualKey, int modifiers = 0) =>
        new() { VirtualKey = virtualKey, Modifiers = modifiers };

    public bool IsValid => VirtualKey != 0;

    public string ToDisplayString(bool german)
    {
        if (!IsValid)
            return german ? "Nicht gesetzt" : "Not set";

        var parts = new List<string>();
        if ((Modifiers & 0x0002) != 0)
            parts.Add(german ? "Strg" : "Ctrl");
        if ((Modifiers & 0x0001) != 0)
            parts.Add("Alt");
        if ((Modifiers & 0x0004) != 0)
            parts.Add(german ? "Umschalt" : "Shift");
        if ((Modifiers & 0x0008) != 0)
            parts.Add("Win");
        parts.Add(VirtualKeyName(VirtualKey));
        return string.Join(" + ", parts);
    }

    public static string VirtualKeyName(int vk) => vk switch
    {
        >= 0x70 and <= 0x87 => "F" + (vk - 0x6F),
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0D => "Enter",
        0x1B => "Esc",
        0x20 => "Space",
        0x21 => "PageUp",
        0x22 => "PageDown",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "Left",
        0x26 => "Up",
        0x27 => "Right",
        0x28 => "Down",
        0x2D => "Insert",
        0x2E => "Delete",
        0x90 => "NumLock",
        0x91 => "ScrollLock",
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x60 and <= 0x69 => "Num" + (vk - 0x60),
        _ => "VK_" + vk.ToString("X2")
    };
}
