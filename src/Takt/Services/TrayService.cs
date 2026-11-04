using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using Takt.Helpers;
using Takt.Native;
using WinForms = System.Windows.Forms;

namespace Takt.Services;

internal enum TrayPresence
{
    Off,
    Active,
    Paused
}

internal sealed class TrayService : IDisposable
{
    private readonly WinForms.NotifyIcon _icon;
    private Icon? _currentIcon;

    public TrayService()
    {
        _icon = new WinForms.NotifyIcon
        {
            Visible = true,
            Text = "Takt"
        };
        _icon.MouseClick += OnClick;
        RebuildMenu();
        ApplyIcon("#5B8DEF", ActivityTone.Idle);
    }

    public event Action? ShowWindowRequested;
    public event Action? ToggleJigglerRequested;
    public event Action? ToggleClickerRequested;
    public event Action? PanicRequested;
    public event Action? ExitRequested;
    public event Action? SettingsRequested;
    public event Action? ToggleCompactRequested;

    public void Attach(Window window) => _ = window;

    public void Refresh(string accentHex, TrayPresence presence, string? detail = null) =>
        Refresh(accentHex, presence == TrayPresence.Paused ? ActivityTone.Paused : presence == TrayPresence.Active ? ActivityTone.Motion : ActivityTone.Idle, detail);

    public void Refresh(string accentHex, ActivityTone tone, string? detail = null)
    {
        ApplyIcon(accentHex, tone);
        var state = tone switch
        {
            ActivityTone.Paused => LocalizationService.Text("Tray.Paused", "Paused"),
            ActivityTone.Idle => LocalizationService.Text("Tray.Idle", "Ready"),
            ActivityTone.Typer => LocalizationService.Text("Tray.ToneTyper", "Typer"),
            ActivityTone.Clicker => LocalizationService.Text("Tray.ToneClicker", "Clicker"),
            _ => LocalizationService.Text("Tray.Active", "Active")
        };
        var text = string.IsNullOrWhiteSpace(detail) ? $"Takt — {state}" : $"Takt — {detail}";
        _icon.Text = text.Length <= 127 ? text : text[..127];
        RebuildMenu();
    }

    public void Notify(string message)
    {
        _icon.BalloonTipTitle = "Takt";
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _currentIcon?.Dispose();
    }

    private void RebuildMenu()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(LocalizationService.Text("Tray.Show", "Show window"), null, (_, _) => ShowWindowRequested?.Invoke());
        menu.Items.Add(LocalizationService.Text("Tray.Settings", "Settings"), null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(LocalizationService.Text("Tray.Jiggler", "Toggle jiggler"), null, (_, _) => ToggleJigglerRequested?.Invoke());
        menu.Items.Add(LocalizationService.Text("Tray.Clicker", "Toggle clicker"), null, (_, _) => ToggleClickerRequested?.Invoke());
        menu.Items.Add(LocalizationService.Text("Tray.Compact", "Compact window"), null, (_, _) => ToggleCompactRequested?.Invoke());
        menu.Items.Add(LocalizationService.Text("Tray.Panic", "Stop all"), null, (_, _) => PanicRequested?.Invoke());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(LocalizationService.Text("Tray.Exit", "Exit"), null, (_, _) => ExitRequested?.Invoke());
        var previous = _icon.ContextMenuStrip;
        _icon.ContextMenuStrip = menu;
        previous?.Dispose();
    }

    private void OnClick(object? sender, WinForms.MouseEventArgs e)
    {
        if (e.Button == WinForms.MouseButtons.Left)
            ShowWindowRequested?.Invoke();
    }

    private void ApplyIcon(string hex, ActivityTone tone)
    {
        var color = ThemeService.Parse(ActivityTonePicker.Hex(tone, hex));
        var accent = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        var created = CreateIcon(accent);
        _icon.Icon = created;
        _currentIcon?.Dispose();
        _currentIcon = created;
    }

    private static Icon CreateIcon(Color accent)
    {
        var fillColor = accent;
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using (var fill = new SolidBrush(fillColor))
            g.FillEllipse(fill, 2, 2, 28, 28);
        using (var pen = new Pen(Color.FromArgb(230, 255, 255, 255), 2.4f))
        {
            g.DrawArc(pen, 8, 8, 16, 16, 210, 120);
            g.DrawLine(pen, 16, 16, 16, 10);
        }

        var handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            Win32.DestroyIcon(handle);
        }
    }
}
