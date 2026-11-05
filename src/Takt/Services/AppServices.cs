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
    private readonly RunClockGate _jigglerClock = new();
    private readonly RunClockGate _clickerClock = new();
    private readonly RunClockGate _typerClock = new();
    private readonly RunClockGate _presenceClock = new();
    private readonly RunClockGate _macroClock = new();
    private readonly PlanCursor _plan = new();
    private readonly ProfileSwitchGate _profiles = new();
    private string? _focusName;
    private DateTime _focusAt;
    private readonly CancellationTokenSource _housekeeping = new();
    private DateTime _presenceTick = DateTime.UtcNow;
    private EngineFlags _held = EngineFlags.None;

    public EngineFlags Held => _held;

    public bool PlanActive => _plan.IsActive;

    public int PlanIndex => _plan.Index;

    public event Action? PauseStateChanged;

    public event Action? PlanChanged;

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
        Clicker.AllowRun = FocusAllows;
        Typer.AllowRun = FocusAllows;
        Clicker.CompletedNaturally += () => QueuePlanFinish(PlanKind.Sequence);
        Typer.CompletedNaturally += () => QueuePlanFinish(PlanKind.Snippet);
        Presence = new PresenceEngine(Settings.Current.Presence, Activity, dispatcher);
        Failsafe = new FailsafeMonitor(Settings.Current, Panic);
        Hotkeys = new HotkeyService(Settings.Current.Hotkeys);
        Hotkeys.SnippetPressed += index => _dispatcher.BeginInvoke(() => PlaySnippet(index));
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
        StopPlanQuiet();
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
        RebindHotkeys();
        ProfileRequested?.Invoke(name);
    }

    public void RebindHotkeys()
    {
        var snippets = Settings.Current.Typer.Snippets?
            .Select(item => item.Hotkey ?? new HotkeyBinding { VirtualKey = 0 })
            .Take(HotkeyService.SnippetSlots)
            .ToArray();
        Hotkeys.SetSnippets(snippets);
        Hotkeys.Rebind();
    }

    public void TogglePlan()
    {
        if (_plan.IsActive)
        {
            StopPlan();
            return;
        }

        var steps = Settings.Current.General.Plan;
        if (steps is null || !_plan.Start(steps.Count, DateTime.UtcNow))
            return;
        BeginPlanStep();
    }

    public void StopPlan()
    {
        if (!_plan.IsActive)
            return;
        _plan.Stop();
        Jiggler.Stop();
        Clicker.Stop();
        Typer.Stop();
        PlanChanged?.Invoke();
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
        StopPlanQuiet();
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
        StopPlanQuiet();
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

    private void StopPlanQuiet()
    {
        if (!_plan.IsActive)
            return;
        _plan.Stop();
        PlanChanged?.Invoke();
    }

    private void ContinuePlan(bool hasNext)
    {
        if (hasNext)
            BeginPlanStep();
        else
        {
            Jiggler.Stop();
            Clicker.Stop();
            Typer.Stop();
            PlanChanged?.Invoke();
        }
    }

    private void BeginPlanStep()
    {
        var guard = 0;
        while (_plan.IsActive && guard++ < PlanLimits.MaxSteps)
        {
            var steps = Settings.Current.General.Plan;
            if (steps is null || _plan.Index < 0 || _plan.Index >= steps.Count)
            {
                _plan.Stop();
                break;
            }

            if (TryStartStep(steps[_plan.Index]))
                break;
            var ticket = _plan.Ticket;
            if (!_plan.Leave(ticket, steps.Count, DateTime.UtcNow, out var hasNext) || !hasNext)
            {
                Jiggler.Stop();
                Clicker.Stop();
                Typer.Stop();
                break;
            }
        }

        PlanChanged?.Invoke();
    }

    private bool TryStartStep(PlanStep step)
    {
        Jiggler.Stop();
        Clicker.Stop();
        Typer.Stop();
        switch (step.Kind)
        {
            case PlanKind.Sequence:
                var sequence = PlanLibrary.FindSequence(Settings.Current.Clicker.SavedSequences, step.Name);
                if (sequence is null)
                    return false;
                Settings.Current.Clicker.Points ??= new();
                SavedLibraries.LoadSequence(sequence, Settings.Current.Clicker.Points);
                Settings.Current.Clicker.PositionMode = ClickPositionMode.Sequence;
                Clicker.Start();
                return true;
            case PlanKind.Snippet:
                var snippet = PlanLibrary.FindSnippet(Settings.Current.Typer.Snippets, step.Name);
                if (snippet is null)
                    return false;
                Settings.Current.Typer.Text = snippet.Text ?? "";
                Typer.Start();
                return true;
            default:
                Jiggler.Start();
                return true;
        }
    }

    private void QueuePlanFinish(PlanKind kind) =>
        _dispatcher.BeginInvoke(() =>
        {
            if (!_plan.IsActive)
                return;
            var steps = Settings.Current.General.Plan;
            if (steps is null || _plan.Index < 0 || _plan.Index >= steps.Count || steps[_plan.Index].Kind != kind)
                return;
            var ticket = _plan.Ticket;
            if (!_plan.Leave(ticket, steps.Count, DateTime.UtcNow, out var hasNext))
                return;
            ContinuePlan(hasNext);
        });

    private void PlaySnippet(int index)
    {
        var snippets = Settings.Current.Typer.Snippets;
        if (snippets is null || index < 0 || index >= snippets.Count)
            return;
        var snippet = snippets[index];
        var text = snippet.Text ?? "";
        if (Typer.IsEnabled && string.Equals(Settings.Current.Typer.Text, text, StringComparison.Ordinal))
        {
            Typer.Stop();
            return;
        }

        Settings.Current.Typer.Text = text;
        if (Typer.IsEnabled)
            Typer.Stop();
        Typer.Start();
    }

    private bool FocusAllows()
    {
        var now = DateTime.UtcNow;
        if (now - _focusAt > TimeSpan.FromMilliseconds(200))
        {
            _focusAt = now;
            try
            {
                _focusName = ForegroundProcess.TryGetName();
            }
            catch
            {
                _focusName = null;
            }
        }

        return FocusGate.Allows(Settings.Current.General.OnlyWhileProcess, _focusName);
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
            var stopJiggler = _jigglerClock.Tick(Jiggler.IsEnabled, now, Settings.Current.Jiggler.StopAfterMinutes, Settings.Current.Jiggler.StopAtTime);
            var stopClicker = _clickerClock.Tick(Clicker.IsEnabled, now, Settings.Current.Clicker.StopAfterMinutes, Settings.Current.Clicker.StopAtTime);
            var stopTyper = _typerClock.Tick(Typer.IsEnabled, now, Settings.Current.Typer.StopAfterMinutes, Settings.Current.Typer.StopAtTime);
            var stopPresence = _presenceClock.Tick(Presence.IsEnabled, now, Settings.Current.Presence.StopAfterMinutes, Settings.Current.Presence.StopAtTime);
            var stopMacro = _macroClock.Tick(Macro.IsPlaying || Macro.IsRecording, now, Settings.Current.Macro.StopAfterMinutes, Settings.Current.Macro.StopAtTime);
            var planSteps = general.Plan;
            var planAdvance = false;
            var planHasNext = false;
            if (_plan.IsActive && planSteps is not null && _plan.Index >= 0 && _plan.Index < planSteps.Count)
            {
                var step = planSteps[_plan.Index];
                planAdvance = _plan.TryTimeout(DateTime.UtcNow, PlanLimits.Seconds(step.Kind, step.Seconds), planSteps.Count, out planHasNext);
            }
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
                if (stopJiggler)
                    Jiggler.Stop();
                if (stopClicker)
                    Clicker.Stop();
                if (stopTyper)
                    Typer.Stop();
                if (stopPresence)
                    Presence.Stop();
                if (stopMacro)
                    Macro.Stop();
                if (planAdvance)
                    ContinuePlan(planHasNext);
                if (!string.IsNullOrEmpty(profile))
                    ApplyNamedProfile(profile);
            });
        }
    }
}
