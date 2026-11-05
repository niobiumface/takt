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
    private readonly Func<string?>? _clipboard;
    private readonly Random _rng = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _generation;
    private int _faultLogged;
    private bool _once;

    public TyperEngine(TyperSettings settings, Dispatcher dispatcher, Func<string?>? clipboard = null)
    {
        _settings = settings;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
    }

    public event Action? CompletedNaturally;

    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private EngineStatus _status = EngineStatus.Stopped;
    [ObservableProperty] private PauseReason _pauseReason;
    [ObservableProperty] private int _typedCount;
    [ObservableProperty] private int _passCount;
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
            if (!_once && _settings.StartDelayMs > 0)
            {
                Publish(true, EngineStatus.Paused, PauseReason.StartDelay, typed, fault: null);
                await Task.Delay(_settings.StartDelayMs, token);
                Publish(true, EngineStatus.Running, PauseReason.None, typed, fault: null);
            }

            var started = DateTime.UtcNow;
            PublishStarted(started);
            var passes = 0;
            while (!token.IsCancellationRequested && IsCurrent(generation))
            {
                if (_once && passes >= 1)
                    break;
                if (IsFinished(started, passes))
                    break;

                string clip;
                try
                {
                    clip = _clipboard?.Invoke() ?? "";
                }
                catch
                {
                    clip = "";
                }

                var text = TyperTemplates.Expand(
                    _settings.Text,
                    DateTime.Now,
                    clip,
                    _settings.DateFormat,
                    _settings.TimeFormat,
                    _settings.DateTimeFormat);
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

                    if (_settings.SimulateTypos && TyperHumanizer.ShouldTypo(value, _settings.TypoPercent, _rng))
                    {
                        if (!KeyboardInput.Type(TyperHumanizer.Mistype(value, _rng)))
                            NoteFault();
                        await Task.Delay(TyperTiming.DelayMs(_settings.CharactersPerMinute, _settings.JitterPercent, _rng), token);
                        KeyboardInput.TapVirtualKey(0x08);
                        await Task.Delay(40, token);
                    }

                    if (!KeyboardInput.Type(value))
                        NoteFault();
                    typed++;
                    PublishCount(typed);
                    var pause = TyperTiming.DelayMs(_settings.CharactersPerMinute, _settings.JitterPercent, _rng)
                                + TyperHumanizer.ExtraPauseMs(value, _settings.PunctuationPauseMs);
                    await Task.Delay(pause, token);
                }

                passes++;
                PublishPasses(passes);
                if (_once)
                    break;
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

    private void PublishStarted(DateTime started)
    {
        void Apply() => RunStartedUtc = started;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }

    private void PublishPasses(int passes)
    {
        void Apply() => PassCount = passes;
        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.BeginInvoke(Apply);
    }

    private void Publish(bool enabled, EngineStatus status, PauseReason reason, int? typed, bool? fault)
    {
        void Apply()
        {
            IsEnabled = enabled;
            Status = status;
            PauseReason = reason;
            if (!enabled)
                RunStartedUtc = null;
            if (typed is int count)
            {
                TypedCount = count;
                if (enabled && count == 0)
                    PassCount = 0;
            }
            if (fault is bool value)
                InputFault = value;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
