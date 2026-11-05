using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Takt.Services;

internal static class TitleBarTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmCornerRound = 2;
    private const int DwmsbtMainWindow = 2;

    public static void Apply(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero)
            return;

        var value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
        TryBackdrop(window, hwnd);
    }

    public static bool TryBackdrop(Window window, nint hwnd)
    {
        if (window.WindowStyle is WindowStyle.None or WindowStyle.ToolWindow || window.AllowsTransparency)
            return false;

        var round = DwmCornerRound;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref round, sizeof(int));
        var mica = DwmsbtMainWindow;
        var hr = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref mica, sizeof(int));
        var ok = hr == 0;
        var chrome = WindowChrome.GetWindowChrome(window);
        if (chrome is not null)
            chrome.GlassFrameThickness = ok ? new Thickness(-1) : new Thickness(0);

        if (ok)
        {
            window.Background = Brushes.Transparent;
            if (PresentationSource.FromVisual(window) is HwndSource source)
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
        }
        else if (window.TryFindResource("WindowBackgroundBrush") is Brush brush)
        {
            window.Background = brush;
        }

        return ok;
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
