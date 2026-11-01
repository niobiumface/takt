using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed partial class MacroEngine : ObservableObject, IDisposable
{
    private const int MaxEvents = 2000;
    private readonly MacroSettings _settings;
    private readonly UserActivityMonitor _activity;
    private readonly Dispatcher _dispatcher;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _generation;
    private DateTime _lastUtc;
    private int _lastX;
    private int _lastY;
    private bool _hasLast;

    public MacroEngine(MacroSettings settings, UserActivityMonitor activity, Dispatcher dispatcher)
    {
        _settings = settings;
        _activity = activity;
        _dispatcher = dispatcher;
        _activity.MouseObserved += OnMouse;
        EventCount = _settings.Events?.Count ?? 0;
    }

    public event Action? CompletedNaturally;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private int _eventCount;

    public void ToggleRecord()
    {
        if (IsRecording)
            StopRecording();
        else
            StartRecording();
    }

    public void TogglePlay()
    {
        if (IsPlaying)
            Stop();
        else
            StartPlayback();
    }

    public void StartRecording()
    {
        Stop();
        _settings.Events ??= new();
        _settings.Events.Clear();
        _lastUtc = DateTime.UtcNow;
        _hasLast = false;
        Publish(recording: true, playing: false);
    }

    public void StopRecording() => Publish(recording: false, playing: false);

    public void StartPlayback()
    {
        if (IsRecording)
            StopRecording();
        if (_settings.Events is null || _settings.Events.Count == 0)
            return;

        CancellationTokenSource cts;
        int generation;
        lock (_gate)
        {
            _cts?.Cancel();
            generation = ++_generation;
            _cts = cts = new CancellationTokenSource();
        }

        Publish(recording: false, playing: true);
        _ = PlayAsync(cts, generation);
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

        Publish(recording: false, playing: false);
    }

    public void Dispose()
    {
        _activity.MouseObserved -= OnMouse;
        Stop();
    }

    private async Task PlayAsync(CancellationTokenSource cts, int generation)
    {
        var token = cts.Token;
        var cancelled = false;
        try
        {
            foreach (var item in _settings.Events.ToArray())
            {
                token.ThrowIfCancellationRequested();
                if (item.DelayMs > 0)
                    await Task.Delay(Math.Min(item.DelayMs, 2000), token);
                Play(item);
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
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

    private void Play(MacroEvent item)
    {
        if (!Enum.TryParse<ClickButton>(item.Button, out var button))
            button = ClickButton.Left;
        switch (item.Kind)
        {
            case "down":
                MouseInput.Down(button);
                break;
            case "up":
                MouseInput.Up(button);
                break;
            case "wheel":
                MouseInput.Wheel(item.WheelDelta == 0 ? PointerButtons.WheelDelta : item.WheelDelta);
                break;
            default:
                MouseInput.MoveAbsolute(item.X, item.Y);
                break;
        }
    }

    private void OnMouse(PhysicalMouse sample)
    {
        if (!IsRecording)
            return;

        var now = DateTime.UtcNow;
        var delay = _hasLast ? (int)(now - _lastUtc).TotalMilliseconds : 0;
        var isMove = UserActivityFilter.IsMouseMove(sample.Message);
        if (isMove && _hasLast &&
            !UserActivityFilter.IsSignificantMove(_lastX, _lastY, sample.X, sample.Y, 2) &&
            delay < 40)
            return;

        _lastUtc = now;
        _lastX = sample.X;
        _lastY = sample.Y;
        _hasLast = true;

        var events = _settings.Events ??= new();
        if (events.Count >= MaxEvents)
        {
            _dispatcher.BeginInvoke(StopRecording);
            return;
        }

        events.Add(new MacroEvent
        {
            DelayMs = Math.Max(0, delay),
            X = sample.X,
            Y = sample.Y,
            Kind = KindFor(sample.Message),
            Button = ButtonFor(sample.Message, sample.WheelDelta),
            WheelDelta = sample.WheelDelta
        });
        var count = events.Count;
        _dispatcher.BeginInvoke(() => EventCount = count);
    }

    private static string KindFor(int message) => message switch
    {
        Win32.WmLButtonDown or Win32.WmRButtonDown or Win32.WmMButtonDown or Win32.WmXButtonDown => "down",
        Win32.WmLButtonUp or Win32.WmRButtonUp or Win32.WmMButtonUp or Win32.WmXButtonUp => "up",
        Win32.WmMouseWheel or Win32.WmMouseHWheel => "wheel",
        _ => "move"
    };

    private static string ButtonFor(int message, int wheel) => message switch
    {
        Win32.WmRButtonDown or Win32.WmRButtonUp => nameof(ClickButton.Right),
        Win32.WmMButtonDown or Win32.WmMButtonUp => nameof(ClickButton.Middle),
        Win32.WmXButtonDown or Win32.WmXButtonUp => nameof(ClickButton.X1),
        Win32.WmMouseWheel or Win32.WmMouseHWheel => wheel < 0 ? nameof(ClickButton.WheelDown) : nameof(ClickButton.WheelUp),
        _ => nameof(ClickButton.Left)
    };

    private bool IsCurrent(int generation)
    {
        lock (_gate)
            return generation == _generation;
    }

    private void Publish(bool recording, bool playing)
    {
        void Apply()
        {
            IsRecording = recording;
            IsPlaying = playing;
            EventCount = _settings.Events?.Count ?? 0;
        }

        if (_dispatcher.CheckAccess())
            Apply();
        else
            _dispatcher.Invoke(Apply);
    }
}
