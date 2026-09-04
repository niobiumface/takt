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
    private readonly object _holdGate = new();
    private CancellationTokenSource? _cts;
    private bool _holding;

    public ClickerEngine(ClickerSettings settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
    }

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private int _clickCount;

    public void Start()
    {
        if (_cts is not null)
            return;

        _cts = new CancellationTokenSource();
        ClickCount = 0;
        SetState(true, EngineStatus.Running);
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        ReleaseHold();
        SetState(false, EngineStatus.Stopped);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            if (_settings.StartDelayMs > 0)
            {
                SetState(true, EngineStatus.Paused);
                await Task.Delay(_settings.StartDelayMs, token);
                SetState(true, EngineStatus.Running);
            }

            if (_settings.Action == ClickAction.Hold)
            {
                MoveIfNeeded();
                lock (_holdGate)
                {
                    MouseInput.Down(_settings.Button);
                    _holding = true;
                }

                if (_settings.RepeatMode == RepeatMode.Duration)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.DurationSeconds)), token);
                else
                    await Task.Delay(Timeout.Infinite, token);

                return;
            }

            var started = DateTime.UtcNow;
            var burstCounter = 0;
            var clicks = 0;

            while (!token.IsCancellationRequested)
            {
                if (IsFinished(started, clicks))
                    break;

                SetState(true, EngineStatus.Running);
                MoveIfNeeded();
                MouseInput.Click(_settings.Button, _settings.Action);
                clicks++;
                var displayed = clicks;
                _ = _dispatcher.BeginInvoke(() => ClickCount = displayed);
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
            // stop requested
        }
        finally
        {
            ReleaseHold();
            if (!token.IsCancellationRequested)
                _dispatcher.Invoke(Stop);
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

    private void MoveIfNeeded()
    {
        if (_settings.PositionMode == ClickPositionMode.Fixed)
            MouseInput.MoveAbsolute(_settings.FixedX, _settings.FixedY);
    }

    private void ReleaseHold()
    {
        lock (_holdGate)
        {
            if (!_holding)
                return;
            MouseInput.Up(_settings.Button);
            _holding = false;
        }
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
