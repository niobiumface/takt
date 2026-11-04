namespace Takt.Helpers;

public static class RunDeadline
{
    public static bool IsDue(DateTime startedLocal, DateTime nowLocal, int afterMinutes, string? clockTime)
    {
        if (afterMinutes > 0 && nowLocal - startedLocal >= TimeSpan.FromMinutes(afterMinutes))
            return true;
        if (!TimeSpan.TryParse(clockTime, out var clock))
            return false;
        var target = nowLocal.Date + clock;
        return startedLocal <= target && nowLocal >= target;
    }

    public static string Describe(DateTime startedLocal, DateTime nowLocal, int afterMinutes, string? clockTime)
    {
        if (afterMinutes > 0)
        {
            var left = TimeSpan.FromMinutes(afterMinutes) - (nowLocal - startedLocal);
            if (left < TimeSpan.Zero)
                left = TimeSpan.Zero;
            return left.ToString(@"hh\:mm\:ss");
        }

        return TimeSpan.TryParse(clockTime, out var clock) ? clock.ToString(@"hh\:mm") : "";
    }
}

public sealed class RunClockGate
{
    private DateTime? _started;
    private bool _fired;

    public bool Tick(bool running, DateTime nowLocal, int afterMinutes, string? clockTime)
    {
        if (!running)
        {
            _started = null;
            _fired = false;
            return false;
        }

        _started ??= nowLocal;
        if (_fired)
            return false;
        if (!RunDeadline.IsDue(_started.Value, nowLocal, afterMinutes, clockTime))
            return false;
        _fired = true;
        return true;
    }

    public string Label(DateTime nowLocal, int afterMinutes, string? clockTime) =>
        _started is DateTime started ? RunDeadline.Describe(started, nowLocal, afterMinutes, clockTime) : "";
}
