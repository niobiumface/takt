using System.Windows;
using System.Windows.Input;
using Takt.Native;

namespace Takt.Views;

public partial class PositionPickerWindow : Window
{
    public int PickedX { get; private set; }
    public int PickedY { get; private set; }

    public event Action<int, int>? Chosen;

    public PositionPickerWindow() : this(coverVirtualScreen: true)
    {
    }

    internal PositionPickerWindow(MonitorDisplay monitor) : this(coverVirtualScreen: false)
    {
        Loaded += (_, _) => ScreenPicker.Place(this, monitor);
    }

    private PositionPickerWindow(bool coverVirtualScreen)
    {
        InitializeComponent();
        if (coverVirtualScreen)
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
        Chosen?.Invoke(PickedX, PickedY);
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
        if (e.Key == Key.Escape)
        {
            Chosen?.Invoke(int.MinValue, int.MinValue);
            try
            {
                DialogResult = false;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }
    }
}
