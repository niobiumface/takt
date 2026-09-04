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
    private CancellationTokenSource? _cts;
    private double _phase;
    private int _originX;
    private int _originY;
    private DateTime _ignoreInputUntil;

    public JigglerEngine(JigglerSettings settings, UserActivityMonitor activity, Dispatcher dispatcher)
    {
        _settings = settings;
        _activity = activity;
        _dispatcher = dispatcher;
    }

    public Func<bool>? ShouldKeepCursorStill { get; set; }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;

    public void Start()
    {
        if (_cts is not null)
            return;

        var pos = MouseInput.GetCursorPos();
        if (_settings.UseFixedCenter)
        {
            _originX = _settings.CenterX;
            _originY = _settings.CenterY;
        }
        else
        {
            _originX = pos.X;
            _originY = pos.Y;
        }
        _phase = 0;
        _ignoreInputUntil = DateTime.UtcNow.AddMilliseconds(750);
        _cts = new CancellationTokenSource();
        SetState(true, EngineStatus.Running);
        ExecutionState.Apply(_settings.PreventSleep, _settings.PreventDisplayOff);
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        ExecutionState.Clear();
        SetState(false, EngineStatus.Stopped);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!CanMove(out var paused))
                {
                    SetState(true, paused ? EngineStatus.Paused : EngineStatus.Running);
                    await Task.Delay(200, token);
                    continue;
                }

                SetState(true, EngineStatus.Running);
                Tick();
                var delay = IntervalCalculator.NextDelayMs(_settings.IntervalMs, _settings.JitterMs, _rng);
                await Task.Delay(delay, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(250, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private bool CanMove(out bool paused)
    {
        paused = false;

        if (_settings.UseSchedule && !ScheduleWindow.IsInside(DateTime.Now, _settings.ScheduleStart, _settings.ScheduleEnd))
        {
            paused = true;
            return false;
        }

        if (_settings.PauseWhenFullscreen && SystemQuery.IsForegroundFullscreen())
        {
            paused = true;
            return false;
        }

        var idle = _activity.IdleTime;
        if (_settings.WaitForIdle && idle.TotalSeconds < Math.Max(1, _settings.IdleThresholdSeconds))
        {
            paused = true;
            return false;
        }

        if (_settings.PauseOnUserInput && DateTime.UtcNow >= _ignoreInputUntil && idle.TotalSeconds < 0.5)
        {
            if (!_settings.UseFixedCenter)
            {
                var pos = MouseInput.GetCursorPos();
                _originX = pos.X;
                _originY = pos.Y;
            }
            paused = true;
            return false;
        }

        return true;
    }

    private void Tick()
    {
        ExecutionState.Apply(_settings.PreventSleep, _settings.PreventDisplayOff);

        var keepStill = ShouldKeepCursorStill?.Invoke() == true;
        var mode = keepStill ? JigglerMode.Invisible : _settings.Mode;
        var radius = Math.Clamp(_settings.RadiusPx, 8, 4000);
        var amp = Math.Min(Math.Clamp(_settings.Amplitude, 1, 50), radius);

        if (mode != JigglerMode.Invisible)
            EnsureInsideRadius(radius);

        switch (mode)
        {
            case JigglerMode.Invisible:
                break;
            case JigglerMode.Circle:
                _phase += Math.PI / 8;
                MoveClamped(RadiusArea.PointOnCircle(_originX, _originY, radius, _phase), radius);
                break;
            case JigglerMode.Figure8:
                _phase += Math.PI / 10;
                MoveClamped((
                    _originX + (int)Math.Round(Math.Sin(_phase) * radius),
                    _originY + (int)Math.Round(Math.Sin(_phase) * Math.Cos(_phase) * radius)
                ), radius);
                break;
            case JigglerMode.Random:
                MoveClamped(RadiusArea.RandomInCircle(_originX, _originY, radius, _rng), radius);
                break;
            default:
                MouseInput.Nudge(amp);
                EnsureInsideRadius(radius);
                break;
        }
    }

    private void EnsureInsideRadius(int radius)
    {
        var pos = MouseInput.GetCursorPos();
        if (RadiusArea.Contains(_originX, _originY, pos.X, pos.Y, radius))
            return;
        MoveClamped((pos.X, pos.Y), radius);
    }

    private void MoveClamped((int X, int Y) point, int radius)
    {
        var clamped = RadiusArea.Clamp(_originX, _originY, point.X, point.Y, radius);
        MouseInput.MoveAbsolute(clamped.X, clamped.Y);
    }

    private void SetState(bool enabled, EngineStatus status)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
