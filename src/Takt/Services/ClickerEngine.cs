using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed partial class ClickerEngine : ObservableObject, IDisposable
{
    private readonly ClickerSettings _settings;
    private readonly Dispatcher _dispatcher;
    private readonly Random _rng = new();
    private readonly object _gate = new();
    private readonly HoldSession _hold = new();
    private readonly UiUpdateGate _countGate = new();
    private CancellationTokenSource? _cts;
    private int _generation;
    private int _pendingCount;
    private int _faultLogged;
    private int _sequenceIndex;
    private bool _timerHeld;
    private bool _once;
    private ClickButton _stepButton;
    private int _pointDelay;

    public event Action? CompletedNaturally;

    public Func<bool>? AllowRun { get; set; }

    public ClickerEngine(ClickerSettings settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
    }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private PauseReason _pauseReason;
    [ObservableProperty] private int _clickCount;
    [ObservableProperty] private bool _inputFault;
    [ObservableProperty] private DateTime? _runStartedUtc;

    public void Start() => Begin(once: false);

    public void StartOnce()
    {
        if (IsEnabled)
            return;
        Begin(once: true);
    }

    private void Begin(bool once)
    {
        CancellationTokenSource cts;
        int generation;
        lock (_gate)
        {
            _cts?.Cancel();
            generation = ++_generation;
            _cts = cts = new CancellationTokenSource();
        }

        _once = once;
        ClickCount = 0;
        _pendingCount = 0;
        _faultLogged = 0;
        _sequenceIndex = 0;
        if (_settings.PreciseTiming && !_timerHeld)
        {
            TimerResolution.Acquire();
            _timerHeld = true;
        }
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

        ReleaseHold();
        if (_timerHeld)
        {
            TimerResolution.Release();
            _timerHeld = false;
        }
        Publish(false, EngineStatus.Stopped, PauseReason.None, fault: null);
    }

    public void ReleaseHeldButton() => ReleaseHold();

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        try
        {
            if (!_once && _settings.StartDelayMs > 0)
            {
                Publish(true, EngineStatus.Paused, PauseReason.StartDelay, fault: null);
                await Task.Delay(_settings.StartDelayMs, token);
                Publish(true, EngineStatus.Running, PauseReason.None, fault: null);
            }

            if (_settings.Action == ClickAction.Hold && !PointerButtons.IsWheel(_settings.Button))
            {
                await RunHoldAsync(token);
                return;
            }

            var started = DateTime.UtcNow;
            PublishStarted(started);
            var burstCounter = 0;
            var clicks = 0;

            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                if (_once && clicks >= 1)
                    break;
                if (IsFinished(started, clicks))
                    break;
                if (!await WaitUntilAllowed(token))
                    break;

                if (!MoveIfNeeded() || !MouseInput.Click(_stepButton, _settings.Action))
                    NoteFault();

                clicks++;
                _pendingCount = clicks;
                PublishCount(clicks, flush: false);
                burstCounter++;

                if (_settings.BurstSize > 0 && burstCounter >= _settings.BurstSize)
                {
                    burstCounter = 0;
                    await Task.Delay(Math.Max(1, _settings.BurstPauseMs), token);
                }

                var delay = _pointDelay > 0
                    ? _pointDelay
                    : IntervalCalculator.NextDelayMs(
                        _settings.IntervalMs,
                        ClickerTiming.JitterMilliseconds(_settings.IntervalMs, _settings.JitterMs, _settings.JitterPercent),
                        _rng);
                await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            ReleaseHold();
            PublishCount(_pendingCount, flush: true);
            if (!cancelled && IsCurrent(generation))
            {
                var quiet = _once;
                _dispatcher.Invoke(() =>
                {
                    if (!quiet)
                        CompletedNaturally?.Invoke();
                    Stop();
                });
            }
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

    private async Task RunHoldAsync(CancellationToken token)
    {
        var started = DateTime.UtcNow;
        PublishStarted(started);
        if (!await WaitUntilAllowed(token))
            return;
        if (_once)
        {
            MoveIfNeeded();
            Press(_stepButton);
            await Task.Delay(Math.Max(1, _settings.HoldDurationMs), token);
            ReleaseHold();
            _pendingCount = 1;
            PublishCount(1, flush: true);
            return;
        }

        var plan = HoldPlan.For(
            _settings.RepeatMode,
            _settings.RepeatCount,
            _settings.DurationSeconds,
            _settings.IntervalMs,
            _settings.HoldDurationMs);
        for (var i = 0; i < plan.Cycles; i++)
        {
            token.ThrowIfCancellationRequested();
            if (!await WaitUntilAllowed(token))
                return;
            MoveIfNeeded();
            Press(_stepButton);
            if (plan.UntilCancelled)
                await Task.Delay(Timeout.Infinite, token);
            else
                await Task.Delay(plan.ActiveHold, token);

            ReleaseHold();
            _pendingCount = i + 1;
            PublishCount(i + 1, flush: false);
            if (i + 1 >= plan.Cycles)
                break;

            var delay = IntervalCalculator.NextDelayMs(_settings.IntervalMs, _settings.JitterMs, _rng);
            await Task.Delay(delay, token);
        }
    }

    private async Task<bool> WaitUntilAllowed(CancellationToken token)
    {
        if (MayRun())
            return true;
        Publish(true, EngineStatus.Paused, PauseReason.Focus, fault: null);
        while (!token.IsCancellationRequested)
        {
            if (MayRun())
            {
                Publish(true, EngineStatus.Running, PauseReason.None, fault: null);
                return true;
            }

            await Task.Delay(250, token);
        }

        return false;
    }

    private bool MayRun()
    {
        try
        {
            return AllowRun?.Invoke() ?? true;
        }
        catch
        {
            return true;
        }
    }

    private bool IsFinished(DateTime started, int clicks)
    {
        return _settings.RepeatMode switch
        {
            RepeatMode.Count => clicks >= Math.Max(1, _settings.RepeatCount),
            RepeatMode.Duration => DateTime.UtcNow - started >= TimeSpan.FromSeconds(Math.Max(1, _settings.DurationSeconds)),
            _ => false
        };
    }

    private bool MoveIfNeeded()
    {
        _stepButton = _settings.Button;
        _pointDelay = 0;
        switch (_settings.PositionMode)
        {
            case ClickPositionMode.Fixed:
                return MouseInput.MoveAbsolute(_settings.FixedX, _settings.FixedY);
            case ClickPositionMode.Sequence:
                var points = _settings.Points;
                if (points is null || points.Count == 0)
                    return true;
                var index = _sequenceIndex % points.Count;
                var point = points[index];
                var step = SequencePlan.For(point, _settings.Button);
                _stepButton = step.Button;
                _pointDelay = step.UsePointDelay ? step.PointDelayMs : 0;
                _sequenceIndex = ClickSequence.NextIndex(index, points.Count);
                return MouseInput.MoveAbsolute(point.X, point.Y);
            default:
                return true;
        }
    }

    private void Press(ClickButton button)
    {
        lock (_hold)
        {
            if (_hold.IsHolding)
                ReleaseHold();
            if (!MouseInput.Down(button))
                NoteFault();
            _hold.Press(button);
        }
    }

    private void ReleaseHold()
    {
        lock (_hold)
        {
            var pressed = _hold.Release();
            if (pressed is not ClickButton button)
                return;
            if (!MouseInput.Up(button))
                NoteFault();
        }
    }

    private bool IsCurrent(int generation)
    {
        lock (_gate)
            return generation == _generation;
    }

    private void PublishCount(int clicks, bool flush)
    {
        if (!flush && !_countGate.Allow(Environment.TickCount64, 100))
            return;
        _ = _dispatcher.BeginInvoke(() => ClickCount = clicks);
    }

    private void NoteFault()
    {
        if (Interlocked.Exchange(ref _faultLogged, 1) == 0)
            AppLog.Error("Clicker SendInput was rejected.");
        void Apply() => InputFault = true;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(Apply);
    }

    private void PublishStarted(DateTime started)
    {
        void Apply() => RunStartedUtc = started;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private void Publish(bool enabled, EngineStatus status, PauseReason reason, bool? fault)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
            PauseReason = reason;
            if (!enabled)
                RunStartedUtc = null;
            if (fault is bool value)
                InputFault = value;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
