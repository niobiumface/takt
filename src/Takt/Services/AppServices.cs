using System.Windows.Threading;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed class AppServices : IDisposable
{
    public SettingsService Settings { get; }
    public UserActivityMonitor Activity { get; }
    public JigglerEngine Jiggler { get; }
    public ClickerEngine Clicker { get; }
    public MacroEngine Macro { get; }
    public TyperEngine Typer { get; }
    public PresenceEngine Presence { get; }
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
        Macro = new MacroEngine(Settings.Current.Macro, Activity, dispatcher);
        Typer = new TyperEngine(Settings.Current.Typer, dispatcher);
        Presence = new PresenceEngine(Settings.Current.Presence, Activity, dispatcher);
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
        Typer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(TyperEngine.IsEnabled))
                UpdateArmed();
        };
        Presence.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PresenceEngine.IsEnabled))
                UpdateArmed();
        };
    }

    public event Action? Stopped;

    public void Panic()
    {
        Failsafe.IsArmed = false;
        Jiggler.Stop();
        Clicker.Stop();
        Macro.Stop();
        Typer.Stop();
        Presence.Stop(persistOff: true);
        Stopped?.Invoke();
    }

    public void Dispose()
    {
        Failsafe.Dispose();
        Activity.Dispose();
        Jiggler.Dispose();
        Clicker.Dispose();
        Macro.Dispose();
        Typer.Dispose();
        Presence.Dispose();
        Hotkeys.Dispose();
        Tray.Dispose();
        Settings.Dispose();
        AwakeCoordinator.ClearAll();
    }

    private void UpdateArmed() =>
        Failsafe.IsArmed = Jiggler.IsEnabled || Clicker.IsEnabled || Typer.IsEnabled || Presence.IsEnabled;
}
