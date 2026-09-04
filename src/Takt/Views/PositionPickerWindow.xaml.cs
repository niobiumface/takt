using System.Windows;
using System.Windows.Input;
using Takt.Native;

namespace Takt.Views;

public partial class PositionPickerWindow : Window
{
    public int PickedX { get; private set; }
    public int PickedY { get; private set; }

    public PositionPickerWindow()
    {
        InitializeComponent();
        OverlayPlacement.CoverVirtualScreen(this);
        MouseMove += (_, _) =>
        {
            var pos = MouseInput.GetCursorPos();
            CoordLabel.Text = $"{pos.X}, {pos.Y}";
        };
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        var pos = MouseInput.GetCursorPos();
        PickedX = pos.X;
        PickedY = pos.Y;
        DialogResult = true;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
