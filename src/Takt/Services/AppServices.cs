using System.Windows.Threading;
using Takt.Models;

namespace Takt.Services;

internal sealed class AppServices : IDisposable
{
    public SettingsService Settings { get; }
    public UserActivityMonitor Activity { get; }
    public JigglerEngine Jiggler { get; }
    public ClickerEngine Clicker { get; }
    public FailsafeMonitor Failsafe { get; }
    public HotkeyService Hotkeys { get; }
    public TrayService Tray { get; }
    public ThemeService Theme { get; } = new();
    public LocalizationService Localization { get; } = new();

    public AppServices(Dispatcher dispatcher)
    {
        Settings = new SettingsService();
        Settings.Load();
        Activity = new UserActivityMonitor();
        Jiggler = new JigglerEngine(Settings.Current.Jiggler, Activity, dispatcher);
        Clicker = new ClickerEngine(Settings.Current.Clicker, dispatcher);
        Failsafe = new FailsafeMonitor(Settings.Current, Panic);
        Hotkeys = new HotkeyService(Settings.Current.Hotkeys);
        Tray = new TrayService();

        Jiggler.ShouldKeepCursorStill = () =>
            Clicker.IsEnabled && Settings.Current.Clicker.PositionMode == ClickPositionMode.CurrentCursor;

        Jiggler.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(JigglerEngine.IsEnabled) or nameof(JigglerEngine.Status))
                UpdateArmed();
        };
        Clicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ClickerEngine.IsEnabled) or nameof(ClickerEngine.Status))
                UpdateArmed();
        };
    }

    public event Action? Stopped;

    public void Panic()
    {
        Failsafe.IsArmed = false;
        Jiggler.Stop();
        Clicker.Stop();
        Stopped?.Invoke();
    }

    public void Dispose()
    {
        Failsafe.Dispose();
        Activity.Dispose();
        Jiggler.Dispose();
        Clicker.Dispose();
        Hotkeys.Dispose();
        Tray.Dispose();
        Settings.Dispose();
        ExecutionStateClear();
    }

    private void UpdateArmed() => Failsafe.IsArmed = Jiggler.IsEnabled || Clicker.IsEnabled;

    private static void ExecutionStateClear() => Native.ExecutionState.Clear();
}
