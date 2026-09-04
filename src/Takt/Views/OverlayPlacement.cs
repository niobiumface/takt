using System.Windows;
using System.Windows.Media;
using Takt.Native;

namespace Takt.Views;

internal static class OverlayPlacement
{
    public static void CoverVirtualScreen(Window window)
    {
        if (window.IsLoaded)
            Apply(window);
        else
            window.Loaded += OnLoaded;

        window.DpiChanged -= OnDpiChanged;
        window.DpiChanged += OnDpiChanged;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            window.Loaded -= OnLoaded;
            Apply(window);
        }
    }

    private static void OnDpiChanged(object sender, DpiChangedEventArgs e)
    {
        if (sender is Window window)
            Apply(window);
    }

    public static (double X, double Y) ToDip(Window window, int pixelX, int pixelY)
    {
        var screen = MouseInput.GetVirtualScreen();
        var dpi = VisualTreeHelper.GetDpi(window);
        return (
            (pixelX - screen.Left) / dpi.DpiScaleX,
            (pixelY - screen.Top) / dpi.DpiScaleY);
    }

    private static void Apply(Window window)
    {
        var screen = MouseInput.GetVirtualScreen();
        var dpi = VisualTreeHelper.GetDpi(window);
        window.Left = screen.Left / dpi.DpiScaleX;
        window.Top = screen.Top / dpi.DpiScaleY;
        window.Width = Math.Max(screen.Width / dpi.DpiScaleX, 1);
        window.Height = Math.Max(screen.Height / dpi.DpiScaleY, 1);
    }
}
