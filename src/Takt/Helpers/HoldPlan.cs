using Takt.Models;

namespace Takt.Helpers;

public readonly record struct HoldPlan(bool UntilCancelled, int Cycles, TimeSpan ActiveHold)
{
    public static HoldPlan For(RepeatMode mode, int repeatCount, int durationSeconds, int intervalMs) =>
        For(mode, repeatCount, durationSeconds, intervalMs, intervalMs);

    public static HoldPlan For(RepeatMode mode, int repeatCount, int durationSeconds, int intervalMs, int holdDurationMs)
    {
        return mode switch
        {
            RepeatMode.Duration => new HoldPlan(false, 1, TimeSpan.FromSeconds(Math.Max(1, durationSeconds))),
            RepeatMode.Count => new HoldPlan(
                false,
                Math.Max(1, repeatCount),
                TimeSpan.FromMilliseconds(Math.Max(1, holdDurationMs))),
            _ => new HoldPlan(true, 1, Timeout.InfiniteTimeSpan)
        };
    }
}
