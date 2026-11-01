using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed partial class TyperEngine : ObservableObject, IDisposable
{
    private readonly TyperSettings _settings;
    private readonly Dispatcher _dispatcher;
    private readonly Random _rng = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _generation;
    private int _faultLogged;

    public TyperEngine(TyperSettings settings, Dispatcher dispatcher)
    {
        _settings = settings;
        _dispatcher = dispatcher;
    }

    public event Action? CompletedNaturally;

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private PauseReason _pauseReason;
    [ObservableProperty] private int _typedCount;
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

        _faultLogged = 0;
        Publish(true, EngineStatus.Running, PauseReason.None, 0, fault: false);
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
                // already finished
            }

            _cts = null;
        }

        Publish(false, EngineStatus.Stopped, PauseReason.None, null, fault: null);
    }

    public void Dispose() => Stop();

    private async Task RunAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        var typed = 0;
        try
        {
            if (_settings.StartDelayMs > 0)
            {
                Publish(true, EngineStatus.Paused, PauseReason.StartDelay, typed, fault: null);
                await Task.Delay(_settings.StartDelayMs, token);
                Publish(true, EngineStatus.Running, PauseReason.None, typed, fault: null);
            }

            var started = DateTime.UtcNow;
            var passes = 0;
            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                if (IsFinished(started, passes))
                    break;

                var text = _settings.Text ?? "";
                if (text.Length == 0)
                {
                    await Task.Delay(200, token);
                    passes++;
                    continue;
                }

                for (var i = 0; i < text.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (IsFinished(started, passes) && _settings.RepeatMode == RepeatMode.Duration)
                        return;

                    var value = text[i];
                    if (value == '\n' && i > 0 && text[i - 1] == '\r')
                        continue;
                    if (value == '\n')
                        value = '\r';

                    if (!KeyboardInput.Type(value))
                        NoteFault();
                    typed++;
                    PublishCount(typed);
                    await Task.Delay(TyperTiming.DelayMs(_settings.CharactersPerMinute, _settings.JitterPercent, _rng), token);
                }

                passes++;
                if (_settings.RepeatMode != RepeatMode.Infinite && IsFinished(started, passes))
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            PublishCount(typed, flush: true);
            if (!cancelled && IsCurrent(generation))
            {
                _dispatcher.Invoke(() =>
                {
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

    private bool IsFinished(DateTime started, int passes)
    {
        return _settings.RepeatMode switch
        {
            RepeatMode.Count => passes >= Math.Max(1, _settings.RepeatCount),
            RepeatMode.Duration => DateTime.UtcNow - started >= TimeSpan.FromSeconds(Math.Max(1, _settings.DurationSeconds)),
            _ => false
        };
    }

    private readonly UiUpdateGate _countGate = new();

    private void PublishCount(int typed, bool flush = false)
    {
        if (!flush && !_countGate.Allow(Environment.TickCount64, 100))
            return;
        _ = _dispatcher.BeginInvoke(() => TypedCount = typed);
    }

    private void NoteFault()
    {
        if (Interlocked.Exchange(ref _faultLogged, 1) == 0)
            AppLog.Error("Typer SendInput was rejected.");
        void Apply() => InputFault = true;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(Apply);
    }

    private bool IsCurrent(int generation)
    {
        lock (_gate)
            return generation == _generation;
    }

    private void Publish(bool enabled, EngineStatus status, PauseReason reason, int? typed, bool? fault)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
            PauseReason = reason;
            if (typed is int count)
                TypedCount = count;
            if (fault is bool value)
                InputFault = value;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
