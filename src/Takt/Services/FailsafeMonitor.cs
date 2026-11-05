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
        _ = RunAsync(_cts);
    }

    public void Dispose()
    {
        var cts = _cts;
        _cts = null;
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // the loop already disposed this source
        }
    }

    private async Task RunAsync(CancellationTokenSource cts)
    {
        var token = cts.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(50, token);

                if (!IsArmed || !_settings.General.FailsafeEnabled)
                    continue;

                if (!MouseInput.TryGetCursorPos(out var pos))
                    continue;
                if (!MouseInput.TryGetMonitor(pos.X, pos.Y, out var monitor))
                    continue;

                var size = SettingsLimits.ClampFailsafe(_settings.General.FailsafeSize);
                if (FailsafeRegion.IsInCorner(pos.X, pos.Y, monitor, _settings.General.FailsafeCorner, size))
                    _onTriggered();
            }
        }
        catch (OperationCanceledException)
        {
            // stopping
        }
        finally
        {
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
}
