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

        ClickCount = 0;
        _pendingCount = 0;
        _faultLogged = 0;
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
        Publish(false, EngineStatus.Stopped, PauseReason.None, fault: null);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        try
        {
            if (_settings.StartDelayMs > 0)
            {
                Publish(true, EngineStatus.Paused, PauseReason.StartDelay, fault: null);
                await Task.Delay(_settings.StartDelayMs, token);
                Publish(true, EngineStatus.Running, PauseReason.None, fault: null);
            }

            if (_settings.Action == ClickAction.Hold)
            {
                await RunHoldAsync(token);
                return;
            }

            var started = DateTime.UtcNow;
            var burstCounter = 0;
            var clicks = 0;

            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                if (IsFinished(started, clicks))
                    break;

                if (!MoveIfNeeded() || !MouseInput.Click(_settings.Button, _settings.Action))
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

                var delay = IntervalCalculator.NextDelayMs(_settings.IntervalMs, _settings.JitterMs, _rng);
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

    private async Task RunHoldAsync(CancellationToken token)
    {
        var plan = HoldPlan.For(_settings.RepeatMode, _settings.RepeatCount, _settings.DurationSeconds, _settings.IntervalMs);
        var button = _settings.Button;
        for (var i = 0; i < plan.Cycles; i++)
        {
            token.ThrowIfCancellationRequested();
            MoveIfNeeded();
            Press(button);
            if (plan.UntilCancelled)
                await Task.Delay(Timeout.Infinite, token);
            else
                await Task.Delay(plan.ActiveHold, token);

            ReleaseHold();
            if (i + 1 >= plan.Cycles)
                break;

            var delay = IntervalCalculator.NextDelayMs(_settings.IntervalMs, _settings.JitterMs, _rng);
            await Task.Delay(delay, token);
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
        if (_settings.PositionMode != ClickPositionMode.Fixed)
            return true;
        return MouseInput.MoveAbsolute(_settings.FixedX, _settings.FixedY);
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
