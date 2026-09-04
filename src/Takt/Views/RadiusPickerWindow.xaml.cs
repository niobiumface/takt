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

    public int CenterX { get; private set; }
    public int CenterY { get; private set; }
    public int RadiusPx { get; private set; }

    public RadiusPickerWindow()
    {
        InitializeComponent();
        OverlayPlacement.CoverVirtualScreen(this);
        HintLabel.Text = LocalizationService.Text("Radius.HintCenter", "Click 1: set the center");
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_centerSet)
            return;

        var pos = MouseInput.GetCursorPos();
        var radius = Distance(_centerX, _centerY, pos.X, pos.Y);
        MeasureLabel.Text = LocalizationService.Text("Radius.Measure", "Radius") + $": {radius} px";
        Draw(radius);
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        var pos = MouseInput.GetCursorPos();
        if (!_centerSet)
        {
            _centerSet = true;
            _centerX = pos.X;
            _centerY = pos.Y;
            HintLabel.Text = LocalizationService.Text("Radius.HintEdge", "Click 2: set the radius · Esc cancels");
            Draw(8);
            return;
        }

        CenterX = _centerX;
        CenterY = _centerY;
        RadiusPx = Math.Max(8, Distance(_centerX, _centerY, pos.X, pos.Y));
        DialogResult = true;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        DialogResult = false;
        Close();
    }

    private void Draw(int radiusPx)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var center = OverlayPlacement.ToDip(this, _centerX, _centerY);
        var r = Math.Max(radiusPx / dpi.DpiScaleX, 4);

        CenterDot.Visibility = Visibility.Visible;
        Canvas.SetLeft(CenterDot, center.X - 4);
        Canvas.SetTop(CenterDot, center.Y - 4);

        RadiusEllipse.Visibility = Visibility.Visible;
        RadiusEllipse.Width = r * 2;
        RadiusEllipse.Height = r * 2;
        Canvas.SetLeft(RadiusEllipse, center.X - r);
        Canvas.SetTop(RadiusEllipse, center.Y - r);
    }

    private static int Distance(int x0, int y0, int x1, int y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
    }
}
