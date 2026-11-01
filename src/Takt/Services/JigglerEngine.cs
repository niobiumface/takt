using System.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed partial class JigglerEngine : ObservableObject, IDisposable
{
    private readonly JigglerSettings _settings;
    private readonly UserActivityMonitor _activity;
    private readonly Dispatcher _dispatcher;
    private readonly Random _rng = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _generation;
    private double _phase;
    private int _originX;
    private int _originY;
    private DateTime _ignoreInputUntil;
    private bool _awakeHeld;
    private bool _paused = true;
    private int _faultLogged;

    public JigglerEngine(JigglerSettings settings, UserActivityMonitor activity, Dispatcher dispatcher)
    {
        _settings = settings;
        _activity = activity;
        _dispatcher = dispatcher;
        _settings.PropertyChanged += OnSettingsChanged;
    }

    public Func<bool>? ShouldKeepCursorStill { get; set; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private PauseReason _pauseReason;
    [ObservableProperty] private bool _inputFault;

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

        if (_settings.UseFixedCenter)
        {
            _originX = _settings.CenterX;
            _originY = _settings.CenterY;
        }
        else if (MouseInput.TryGetCursorPos(out var pos))
        {
            _originX = pos.X;
            _originY = pos.Y;
        }
        else
        {
            NoteFault();
        }

        _phase = 0;
        _faultLogged = 0;
        _ignoreInputUntil = DateTime.UtcNow.AddMilliseconds(750);
        Publish(true, EngineStatus.Running, PauseReason.None, fault: false);
        _ = RunAsync(cts, generation);
    }

    public void Stop()
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
                // the loop already finished this source
            }

            _cts = null;
        }

        ClearAwake();
        Publish(false, EngineStatus.Stopped, PauseReason.None, fault: null);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        try
        {
            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                try
                {
                    if (!CanMove(out var reason))
                    {
                        Publish(true, EngineStatus.Paused, reason, fault: null);
                        UpdateAwake(paused: true, generation);
                        await Task.Delay(200, token);
                        continue;
                    }

                    Publish(true, EngineStatus.Running, PauseReason.None, fault: null);
                    UpdateAwake(paused: false, generation);
                    if (!Tick())
                        NoteFault();

                    var delay = IntervalCalculator.NextDelayMs(_settings.IntervalMs, _settings.JitterMs, _rng);
                    await Task.Delay(delay, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    AppLog.Error("Jiggler tick failed.", ex);
                    NoteFault();
                    try
                    {
                        await Task.Delay(250, token);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        break;
                    }
                }
            }
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

    private bool CanMove(out PauseReason reason)
    {
        reason = PauseReason.None;

        if (_settings.UseSchedule && !ScheduleWindow.IsInside(DateTime.Now, _settings.ScheduleStart, _settings.ScheduleEnd))
        {
            reason = PauseReason.Schedule;
            return false;
        }

        if (_settings.PauseWhenFullscreen && SystemQuery.IsForegroundFullscreen())
        {
            reason = PauseReason.Fullscreen;
            return false;
        }

        var idle = _activity.IdleTime;
        if (_settings.WaitForIdle && idle.TotalSeconds < Math.Max(1, _settings.IdleThresholdSeconds))
        {
            reason = PauseReason.WaitingForIdle;
            return false;
        }

        if (_settings.PauseOnUserInput && DateTime.UtcNow >= _ignoreInputUntil && idle.TotalSeconds < 0.5)
        {
            if (!_settings.UseFixedCenter && MouseInput.TryGetCursorPos(out var pos))
            {
                _originX = pos.X;
                _originY = pos.Y;
            }

            reason = PauseReason.UserInput;
            return false;
        }

        return true;
    }

    private bool Tick()
    {
        var keepStill = ShouldKeepCursorStill?.Invoke() == true;
        var mode = keepStill ? JigglerMode.Invisible : _settings.Mode;
        var radius = SettingsLimits.ClampRadius(_settings.RadiusPx);
        var amp = Math.Min(SettingsLimits.ClampAmplitude(_settings.Amplitude), radius);
        var ok = true;

        if (mode != JigglerMode.Invisible)
            ok &= EnsureInsideRadius(radius);

        switch (mode)
        {
            case JigglerMode.Invisible:
                break;
            case JigglerMode.Circle:
                _phase += Math.PI / 8;
                ok &= MoveClamped(RadiusArea.PointOnCircle(_originX, _originY, radius, _phase), radius);
                break;
            case JigglerMode.Figure8:
                _phase += Math.PI / 10;
                ok &= MoveClamped((
                    _originX + (int)Math.Round(Math.Sin(_phase) * radius),
                    _originY + (int)Math.Round(Math.Sin(_phase) * Math.Cos(_phase) * radius)
                ), radius);
                break;
            case JigglerMode.Random:
                // Random lands anywhere inside the operating radius. Amplitude is only used by nudge.
                ok &= MoveClamped(RadiusArea.RandomInCircle(_originX, _originY, radius, _rng), radius);
                break;
            default:
                ok &= MouseInput.Nudge(amp);
                ok &= EnsureInsideRadius(radius);
                break;
        }

        return ok;
    }

    private bool EnsureInsideRadius(int radius)
    {
        if (!MouseInput.TryGetCursorPos(out var pos))
            return false;
        if (RadiusArea.Contains(_originX, _originY, pos.X, pos.Y, radius))
            return true;
        return MoveClamped((pos.X, pos.Y), radius);
    }

    private bool MoveClamped((int X, int Y) point, int radius)
    {
        var clamped = RadiusArea.Clamp(_originX, _originY, point.X, point.Y, radius);
        return MouseInput.MoveAbsolute(clamped.X, clamped.Y);
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsEnabled)
            return;
        if (e.PropertyName is nameof(JigglerSettings.PreventSleep) or nameof(JigglerSettings.PreventDisplayOff))
            UpdateAwake(_paused, CurrentGeneration());
    }

    private void UpdateAwake(bool paused, int generation)
    {
        void Apply()
        {
            if (!IsCurrent(generation))
                return;

            _paused = paused;
            var request = AwakePolicy.ShouldRequest(IsEnabled, paused, _settings.PreventSleep, _settings.PreventDisplayOff);
            if (request)
                ExecutionState.Apply(_settings.PreventSleep, _settings.PreventDisplayOff);
            else if (_awakeHeld)
                ExecutionState.Clear();
            _awakeHeld = request;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private void ClearAwake()
    {
        void Apply()
        {
            if (_awakeHeld)
                ExecutionState.Clear();
            _awakeHeld = false;
            _paused = true;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private int CurrentGeneration()
    {
        lock (_gate)
            return _generation;
    }

    private bool IsCurrent(int generation)
    {
        lock (_gate)
            return generation == _generation;
    }

    private void NoteFault()
    {
        if (Interlocked.Exchange(ref _faultLogged, 1) == 0)
            AppLog.Error("Jiggler SendInput was rejected.");
        void Apply() => InputFault = true;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(Apply);
    }

    private void Publish(bool enabled, EngineStatus status, PauseReason reason, bool? fault)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
            PauseReason = reason;
            if (fault is bool value)
                InputFault = value;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
