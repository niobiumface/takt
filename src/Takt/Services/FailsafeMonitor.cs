using Takt.Helpers;
using Takt.Models;
using Takt.Native;

namespace Takt.Services;

internal sealed class FailsafeMonitor : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _onTriggered;
    private CancellationTokenSource? _cts;

    public FailsafeMonitor(AppSettings settings, Action onTriggered)
    {
        _settings = settings;
        _onTriggered = onTriggered;
    }

    public bool IsArmed { get; set; }

    public void Start()
    {
        if (_cts is not null)
            return;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(50, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsArmed || !_settings.General.FailsafeEnabled)
                continue;

            var pos = MouseInput.GetCursorPos();
            var screen = MouseInput.GetVirtualScreen();
            if (FailsafeRegion.IsInCorner(pos.X, pos.Y, screen, _settings.General.FailsafeCorner, _settings.General.FailsafeSize))
                _onTriggered();
        }
    }
}
