using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Takt.Services;

internal static class TitleBarTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    public static void Apply(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero)
            return;

        var value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
    }

    public static void ApplyAll(bool dark)
    {
        var app = Application.Current;
        if (app is null)
            return;
        foreach (Window window in app.Windows)
            Apply(window, dark);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}
