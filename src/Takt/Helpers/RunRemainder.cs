using Takt.Models;

namespace Takt.Helpers;

public static class RunRemainder
{
    public static int? CountLeft(RepeatMode mode, int done, int total)
    {
        if (mode != RepeatMode.Count)
            return null;
        return Math.Max(0, Math.Max(1, total) - Math.Max(0, done));
    }

    public static int? SecondsLeft(RepeatMode mode, DateTime? startedUtc, DateTime nowUtc, int durationSeconds)
    {
        if (mode != RepeatMode.Duration || startedUtc is null)
            return null;
        var elapsed = (int)(nowUtc - startedUtc.Value).TotalSeconds;
        return Math.Max(0, Math.Max(1, durationSeconds) - Math.Max(0, elapsed));
    }
}
