namespace Takt.Helpers;

public sealed class UiUpdateGate
{
    private long _next;

    public bool Allow(long nowMs, int intervalMs)
    {
        if (nowMs < _next)
            return false;
        _next = nowMs + Math.Max(1, intervalMs);
        return true;
    }
}
