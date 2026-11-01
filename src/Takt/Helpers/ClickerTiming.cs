namespace Takt.Helpers;

public static class ClickerTiming
{
    public static int JitterMilliseconds(int intervalMs, int jitterMs, int jitterPercent)
    {
        var interval = Math.Max(intervalMs, 1);
        if (jitterPercent > 0)
            return interval * Math.Clamp(jitterPercent, 0, 100) / 100;
        return Math.Max(0, jitterMs);
    }
}
