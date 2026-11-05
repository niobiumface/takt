using System.Windows;
using Takt.Native;

namespace Takt.Views;

internal static class ScreenPicker
{
    public static void Place(Window window, MonitorDisplay monitor)
    {
        var scaleX = Math.Max(monitor.DpiX / 96d, 0.5);
        var scaleY = Math.Max(monitor.DpiY / 96d, 0.5);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = monitor.Pixels.Left / scaleX;
        window.Top = monitor.Pixels.Top / scaleY;
        window.Width = Math.Max((monitor.Pixels.Right - monitor.Pixels.Left) / scaleX, 1);
        window.Height = Math.Max((monitor.Pixels.Bottom - monitor.Pixels.Top) / scaleY, 1);
    }

    public static bool TryPickPosition(out int x, out int y)
    {
        var monitors = MonitorQuery.All();
        if (monitors.Count <= 1)
        {
            var dialog = monitors.Count == 1 ? new PositionPickerWindow(monitors[0]) : new PositionPickerWindow();
            var accepted = dialog.ShowDialog() == true;
            x = dialog.PickedX;
            y = dialog.PickedY;
            return accepted;
        }

        int pickedX = 0, pickedY = 0;
        var acceptedMulti = false;
        var windows = monitors.Select(monitor => new PositionPickerWindow(monitor)).ToList();
        void Finish(int px, int py, bool ok)
        {
            if (px != int.MinValue)
            {
                pickedX = px;
                pickedY = py;
                acceptedMulti = ok;
            }
            foreach (var window in windows)
            {
                if (window.IsVisible)
                    window.Close();
            }
        }

        foreach (var window in windows)
        {
            window.Chosen += (px, py) => Finish(px, py, px != int.MinValue);
            window.KeyDown += (_, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Escape)
                    Finish(int.MinValue, 0, false);
            };
        }

        foreach (var window in windows.Skip(1))
            window.Show();
        windows[0].ShowDialog();
        x = pickedX;
        y = pickedY;
        return acceptedMulti;
    }

    public static bool TryPickRadius(out int centerX, out int centerY, out int radius)
    {
        var monitors = MonitorQuery.All();
        if (monitors.Count <= 1)
        {
            var dialog = monitors.Count == 1 ? new RadiusPickerWindow(monitors[0]) : new RadiusPickerWindow();
            var accepted = dialog.ShowDialog() == true;
            centerX = dialog.CenterX;
            centerY = dialog.CenterY;
            radius = dialog.RadiusPx;
            return accepted;
        }

        int cx = 0, cy = 0, r = 0;
        var acceptedMulti = false;
        RadiusPickerWindow.ResetSharedCenter();
        var windows = monitors.Select(monitor => new RadiusPickerWindow(monitor)).ToList();
        void Finish(int x, int y, int pickedRadius, bool ok)
        {
            if (ok)
            {
                cx = x;
                cy = y;
                r = pickedRadius;
                acceptedMulti = true;
            }
            foreach (var window in windows)
            {
                if (window.IsVisible)
                    window.Close();
            }
        }

        foreach (var window in windows)
            window.Chosen += (x, y, pickedRadius) => Finish(x, y, pickedRadius, pickedRadius > 0);

        foreach (var window in windows.Skip(1))
            window.Show();
        windows[0].ShowDialog();
        centerX = cx;
        centerY = cy;
        radius = r;
        return acceptedMulti;
    }
}
