using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Takt.Native;
using Takt.Services;

namespace Takt.Views;

public partial class RadiusPickerWindow : Window
{
    private bool _centerSet;
    private int _centerX;
    private int _centerY;
    private MonitorDisplay? _monitor;

    public event Action<int, int, int>? Chosen;

    public int CenterX { get; private set; }
    public int CenterY { get; private set; }
    public int RadiusPx { get; private set; }

    public RadiusPickerWindow() : this(coverVirtualScreen: true)
    {
    }

    internal RadiusPickerWindow(MonitorDisplay monitor) : this(coverVirtualScreen: false)
    {
        _monitor = monitor;
        Loaded += (_, _) => ScreenPicker.Place(this, monitor);
    }

    private RadiusPickerWindow(bool coverVirtualScreen)
    {
        InitializeComponent();
        if (coverVirtualScreen)
            OverlayPlacement.CoverVirtualScreen(this);
        HintLabel.Text = LocalizationService.Text("Radius.HintCenter", "Click 1: set the center");
    }

    public static void ResetSharedCenter()
    {
        SharedCenterSet = false;
        SharedX = 0;
        SharedY = 0;
    }

    private static bool SharedCenterSet;
    private static int SharedX;
    private static int SharedY;

    private bool HasCenter => _monitor is null ? _centerSet : SharedCenterSet;

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!HasCenter)
            return;

        var originX = _monitor is null ? _centerX : SharedX;
        var originY = _monitor is null ? _centerY : SharedY;
        var pos = MouseInput.GetCursorPos();
        var radius = Distance(originX, originY, pos.X, pos.Y);
        MeasureLabel.Text = LocalizationService.Text("Radius.Measure", "Radius") + $": {radius} px";
        Draw(radius);
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        var pos = MouseInput.GetCursorPos();
        if (!HasCenter)
        {
            _centerSet = true;
            _centerX = pos.X;
            _centerY = pos.Y;
            SharedCenterSet = true;
            SharedX = pos.X;
            SharedY = pos.Y;
            HintLabel.Text = LocalizationService.Text("Radius.HintEdge", "Click 2: set the radius · Esc cancels");
            Draw(8);
            return;
        }

        CenterX = _monitor is null ? _centerX : SharedX;
        CenterY = _monitor is null ? _centerY : SharedY;
        RadiusPx = Math.Max(8, Distance(_centerX, _centerY, pos.X, pos.Y));
        Chosen?.Invoke(CenterX, CenterY, RadiusPx);
        try
        {
            DialogResult = true;
        }
        catch (InvalidOperationException)
        {
            Close();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Chosen?.Invoke(int.MinValue, int.MinValue, 0);
        try
        {
            DialogResult = false;
        }
        catch (InvalidOperationException)
        {
            Close();
        }
    }

    private void Draw(int radiusPx)
    {
        double centerX;
        double centerY;
        double radius;
        if (_monitor is MonitorDisplay monitor)
        {
            var scaleX = Math.Max(monitor.DpiX / 96d, 0.5);
            var scaleY = Math.Max(monitor.DpiY / 96d, 0.5);
            centerX = (SharedX - monitor.Pixels.Left) / scaleX;
            centerY = (SharedY - monitor.Pixels.Top) / scaleY;
            radius = Math.Max(radiusPx / scaleX, 4);
        }
        else
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var center = OverlayPlacement.ToDip(this, _centerX, _centerY);
            centerX = center.X;
            centerY = center.Y;
            radius = Math.Max(radiusPx / dpi.DpiScaleX, 4);
        }

        var r = radius;

        CenterDot.Visibility = Visibility.Visible;
        Canvas.SetLeft(CenterDot, centerX - 4);
        Canvas.SetTop(CenterDot, centerY - 4);

        RadiusEllipse.Visibility = Visibility.Visible;
        RadiusEllipse.Width = r * 2;
        RadiusEllipse.Height = r * 2;
        Canvas.SetLeft(RadiusEllipse, centerX - r);
        Canvas.SetTop(RadiusEllipse, centerY - r);
    }

    private static int Distance(int x0, int y0, int x1, int y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
    }
}
