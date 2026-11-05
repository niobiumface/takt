using System.Windows;
using System.Windows.Threading;
using Takt.Helpers;
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
    public StatsStore Stats { get; } = new();

    public event Action<string>? ProfileRequested;
    public event Action? TimerDue;
    public event Action<string>? DeadlineChanged;

    private readonly Dispatcher _dispatcher;
    private readonly RunClockGate _clock = new();
    private readonly ProfileSwitchGate _profiles = new();
    private readonly CancellationTokenSource _housekeeping = new();
    private DateTime _presenceTick = DateTime.UtcNow;
    private EngineFlags _held = EngineFlags.None;

    public EngineFlags Held => _held;

    public event Action? PauseStateChanged;

    public AppServices(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        Settings = new SettingsService();
        Settings.Load();
        Stats.Load();
        Activity = new UserActivityMonitor();
        Jiggler = new JigglerEngine(Settings.Current.Jiggler, Activity, dispatcher);
        Clicker = new ClickerEngine(Settings.Current.Clicker, dispatcher);
        Macro = new MacroEngine(Settings.Current.Macro, Activity, dispatcher, WindowAnchorQuery.TryForeground, WindowAnchorQuery.TryFind);
        Typer = new TyperEngine(Settings.Current.Typer, dispatcher, ReadClipboard);
        Presence = new PresenceEngine(Settings.Current.Presence, Activity, dispatcher);
        Failsafe = new FailsafeMonitor(Settings.Current, Panic);
        Hotkeys = new HotkeyService(Settings.Current.Hotkeys);
        Tray = new TrayService();

        Jiggler.ShouldKeepCursorStill = () =>
            Clicker.IsEnabled && Settings.Current.Clicker.PositionMode == ClickPositionMode.CurrentCursor;
        Jiggler.Schedule = () => WorkScheduleEvaluator.Evaluate(DateTime.Now, Settings.Current.Work);
        Presence.Schedule = Jiggler.Schedule;

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
        _ = HousekeepingAsync(_housekeeping.Token);
    }

    public void StopForTimer()
    {
        _held = EngineFlags.None;
        Jiggler.Stop();
        Clicker.Stop();
        Macro.Stop();
        Typer.Stop();
        Presence.Stop();
        PauseStateChanged?.Invoke();
        TimerDue?.Invoke();
    }

    public void ApplyNamedProfile(string name)
    {
        var profile = Settings.Current.Profiles.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
            return;
        ProfileApplicator.Apply(profile, Settings.Current);
        Theme.Apply(Settings.Current);
        Hotkeys.Rebind();
        ProfileRequested?.Invoke(name);
    }

    public event Action? Stopped;

    public PauseAction TogglePause()
    {
        if (Macro.IsRecording && _held == EngineFlags.None &&
            PauseResume.Capture(Jiggler.IsEnabled, Clicker.IsEnabled, Typer.IsEnabled, Presence.IsEnabled, Macro.IsPlaying) == EngineFlags.None)
        {
            Macro.Stop();
            return PauseAction.Idle;
        }

        var action = PauseResume.Decide(
            PauseResume.Capture(Jiggler.IsEnabled, Clicker.IsEnabled, Typer.IsEnabled, Presence.IsEnabled, Macro.IsPlaying),
            _held);
        if (action == PauseAction.Resumed)
        {
            var held = _held;
            _held = EngineFlags.None;
            if (held.HasFlag(EngineFlags.Jiggler))
                Jiggler.Start();
            if (held.HasFlag(EngineFlags.Clicker))
                Clicker.Start();
            if (held.HasFlag(EngineFlags.Typer))
                Typer.Start();
            if (held.HasFlag(EngineFlags.Presence))
            {
                Settings.Current.Presence.Enabled = true;
                Presence.Start();
            }

            if (held.HasFlag(EngineFlags.Macro))
                Macro.StartPlayback();
            PauseStateChanged?.Invoke();
            return action;
        }

        if (action != PauseAction.Suspended)
            return PauseAction.Idle;

        _held = PauseResume.Capture(Jiggler.IsEnabled, Clicker.IsEnabled, Typer.IsEnabled, Presence.IsEnabled, Macro.IsPlaying);
        Jiggler.Stop();
        Clicker.Stop();
        Macro.Stop();
        Typer.Stop();
        Presence.Stop();
        PauseStateChanged?.Invoke();
        return action;
    }

    public void ForgetPause()
    {
        if (_held == EngineFlags.None)
            return;
        _held = EngineFlags.None;
        PauseStateChanged?.Invoke();
    }

    public void Panic()
    {
        _held = EngineFlags.None;
        Failsafe.IsArmed = false;
        Jiggler.Stop();
        Clicker.Stop();
        Macro.Stop();
        Typer.Stop();
        Presence.Stop(persistOff: true);
        PauseStateChanged?.Invoke();
        Stopped?.Invoke();
    }

    public void Dispose()
    {
        _housekeeping.Cancel();
        try
        {
            Stats.Flush();
        }
        catch
        {
            AppLog.Error("Stats file could not be saved.");
        }

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

    private static string ReadClipboard()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : "";
        }
        catch
        {
            return "";
        }
    }

    private bool AnyRunning() =>
        Jiggler.IsEnabled || Clicker.IsEnabled || Typer.IsEnabled || Presence.IsEnabled || Macro.IsPlaying || Macro.IsRecording;

    private async Task HousekeepingAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(250, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var general = Settings.Current.General;
            var now = DateTime.Now;
            var running = AnyRunning();
            var due = _clock.Tick(running, now, general.StopAfterMinutes, general.StopAtTime);
            var label = running ? _clock.Label(now, general.StopAfterMinutes, general.StopAtTime) : "";
            var rules = general.AppRules.Select(rule => (rule.ProcessName, rule.ProfileName, rule.Enabled)).ToArray();
            var profile = _profiles.Tick(general.AutoProfile, ForegroundProcess.TryGetName(), rules, DateTime.UtcNow, general.ProfileDebounceMs);
            if (!general.StatsOptOut && Presence.IsEnabled && Presence.Status == EngineStatus.Running &&
                DateTime.UtcNow - _presenceTick >= TimeSpan.FromSeconds(1))
            {
                _presenceTick = DateTime.UtcNow;
                Stats.Add(now, 0, 0, 1);
            }

            try
            {
                Stats.Flush();
            }
            catch
            {
                AppLog.Error("Stats file could not be saved.");
            }

            _ = _dispatcher.BeginInvoke(() =>
            {
                DeadlineChanged?.Invoke(label);
                if (due)
                    StopForTimer();
                if (!string.IsNullOrEmpty(profile))
                    ApplyNamedProfile(profile);
            });
        }
    }
}
