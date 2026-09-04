namespace Takt.Helpers;

public static class IntervalCalculator
{
    public static int NextDelayMs(int intervalMs, int jitterMs, Random rng)
    {
        var interval = Math.Max(intervalMs, 1);
        if (jitterMs <= 0)
            return interval;

        var jitter = Math.Min(jitterMs, interval - 1);
        if (jitter <= 0)
            return interval;

        var offset = rng.Next(-jitter, jitter + 1);
        return Math.Max(1, interval + offset);
    }
}
