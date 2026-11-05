using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed partial class PresenceEngine : ObservableObject, IDisposable
{
    private readonly PresenceSettings _settings;
    private readonly UserActivityMonitor _activity;
    private readonly Dispatcher _dispatcher;
    private readonly Random _rng = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _generation;

    public Func<ScheduleEffect>? Schedule { get; set; }

    public PresenceEngine(PresenceSettings settings, UserActivityMonitor activity, Dispatcher dispatcher)
    {
        _settings = settings;
        _activity = activity;
        _dispatcher = dispatcher;
    }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private PauseReason _pauseReason;
    [ObservableProperty] private int _idleSeconds;
    [ObservableProperty] private PresenceWarning _warning;
    [ObservableProperty] private bool _sessionLocked;

    public void Start()
    {
        CancellationTokenSource cts;
        int generation;
        lock (_gate)
        {
            _cts?.Cancel();
            generation = ++_generation;
            _cts = cts = new CancellationTokenSource();
        }

        SessionLocked = false;
        Publish(true, EngineStatus.Running, PauseReason.None, PresenceWarning.None);
        _ = RunAsync(cts, generation);
    }

    public void Stop(bool persistOff = false)
    {
        lock (_gate)
        {
            _generation++;
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already finished
            }

            _cts = null;
        }

        if (persistOff)
            _settings.Enabled = false;
        ClearAwake();
        Publish(false, EngineStatus.Stopped, PauseReason.None, PresenceWarning.None);
    }

    public void ReportSessionLock(bool locked)
    {
        void Apply()
        {
            SessionLocked = locked;
            if (locked && IsEnabled)
                Warning = PresenceWarning.SessionLocked;
            else if (!locked && Warning == PresenceWarning.SessionLocked)
                Warning = PresenceWarning.None;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(Apply);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        try
        {
            var policy = SystemQuery.PolicyLockTimeoutSeconds();
            if (policy is > 0)
                Publish(true, EngineStatus.Running, PauseReason.None, PresenceWarning.PolicyLock);

            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                var effect = Schedule?.Invoke() ?? LegacyEffect();
                if (effect is ScheduleEffect.Pause or ScheduleEffect.Stop)
                {
                    if (effect == ScheduleEffect.Stop)
                    {
                        _ = _dispatcher.BeginInvoke(() => Stop());
                        return;
                    }

                    var pausedWarning = SessionLocked
                        ? PresenceWarning.SessionLocked
                        : policy is > 0 ? PresenceWarning.PolicyLock : PresenceWarning.None;
                    Publish(true, EngineStatus.Paused, PauseReason.Schedule, pausedWarning);
                    UpdateAwake(false, generation);
                    await Task.Delay(1000, token);
                    continue;
                }

                var warning = SessionLocked
                    ? PresenceWarning.SessionLocked
                    : policy is > 0 ? PresenceWarning.PolicyLock : PresenceWarning.None;

                var resumeAfter = Math.Max(1, _settings.ResumeIdleSeconds);
                if (_settings.PauseOnUserInput && _activity.IdleTime.TotalSeconds < resumeAfter)
                {
                    Publish(true, EngineStatus.Paused, PauseReason.UserInput, warning);
                    UpdateAwake(true, generation);
                    await Task.Delay(500, token);
                    continue;
                }

                var moved = MoveForPresence();
                var idleKnown = SystemQuery.TryGetIdleTime(out var idle);
                if (!moved || !idleKnown || !IdleVerification.InputRegistered(idle))
                {
                    if (_settings.UseFallbackKey && _settings.FallbackKey != PresenceKey.None)
                    {
                        KeyboardInput.TapVirtualKey(VirtualKey(_settings.FallbackKey));
                        idleKnown = SystemQuery.TryGetIdleTime(out idle);
                    }

                    if (!idleKnown || !IdleVerification.InputRegistered(idle))
                    {
                        if (warning == PresenceWarning.None)
                            warning = PresenceWarning.IdleNotReset;
                        AppLog.Error("Presence input did not reset the system idle timer.");
                    }
                }

                if (idleKnown)
                    PublishIdle(idle);

                Publish(true, EngineStatus.Running, PauseReason.None, warning);
                UpdateAwake(true, generation);

                var delay = IntervalCalculator.NextDelayMs(
                    PresenceLimits.ClampInterval(_settings.IntervalMs),
                    PresenceLimits.ClampJitter(_settings.JitterMs),
                    _rng);
                var left = delay;
                while (left > 0 && !token.IsCancellationRequested)
                {
                    var slice = Math.Min(1000, left);
                    await Task.Delay(slice, token);
                    if (SystemQuery.TryGetIdleTime(out var sliceIdle))
                        PublishIdle(sliceIdle);
                    left -= slice;
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            AppLog.Error("Presence tick failed.", ex);
        }
        finally
        {
            if (!cancelled && IsCurrent(generation))
                _dispatcher.Invoke(Stop);
            try
            {
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }
        }
    }

    private ScheduleEffect LegacyEffect()
    {
        if (!_settings.UseWorkHours)
            return ScheduleEffect.Run;
        return ScheduleWindow.IsInside(DateTime.Now, _settings.ScheduleStart, _settings.ScheduleEnd, _settings.Days)
            ? ScheduleEffect.Run
            : ScheduleEffect.Pause;
    }

    private bool MoveForPresence()
    {
        switch (_settings.Motion)
        {
            case PresenceMotion.SmallMove:
                return MouseInput.Nudge(4);
            case PresenceMotion.SmallCircle:
                _phase += Math.PI / 6;
                if (!MouseInput.TryGetCursorPos(out var pos))
                    return false;
                var x = pos.X + (int)Math.Round(Math.Cos(_phase) * 6);
                var y = pos.Y + (int)Math.Round(Math.Sin(_phase) * 6);
                return MouseInput.MoveAbsolute(x, y);
            default:
                return MouseInput.PulseRelative(1, 0);
        }
    }

    private double _phase;

    private static ushort VirtualKey(PresenceKey key) => key switch
    {
        PresenceKey.Shift => KeyboardInput.VirtualKeyShift,
        PresenceKey.Control => KeyboardInput.VirtualKeyControl,
        _ => KeyboardInput.VirtualKeyF15
    };

    private void PublishIdle(TimeSpan idle)
    {
        var seconds = Math.Max(0, (int)idle.TotalSeconds);
        if (seconds == IdleSeconds)
            return;
        _ = _dispatcher.BeginInvoke(() => IdleSeconds = seconds);
    }

    private void UpdateAwake(bool active, int generation)
    {
        void Apply()
        {
            if (!IsCurrent(generation))
                return;
            AwakeCoordinator.SetPresence(
                active && IsEnabled,
                _settings.PreventSleep,
                _settings.PreventDisplayOff || _settings.PreventScreensaver);
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private void ClearAwake()
    {
        void Apply() => AwakeCoordinator.SetPresence(false, false, false);
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private bool IsCurrent(int generation)
    {
        lock (_gate)
            return generation == _generation;
    }

    private void Publish(bool enabled, EngineStatus status, PauseReason reason, PresenceWarning warning)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
            PauseReason = reason;
            if (warning == PresenceWarning.SessionLocked || SessionLocked)
                Warning = PresenceWarning.SessionLocked;
            else
                Warning = warning;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
