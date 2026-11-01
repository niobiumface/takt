namespace Takt.Helpers;

public static class PresenceLimits
{
    public const int DefaultIntervalMs = 45_000;
    public const int MinIntervalMs = 5_000;
    public const int MaxIntervalMs = 120_000;
    public const int MaxJitterMs = 30_000;

    public static int ClampInterval(int value) => Math.Clamp(value, MinIntervalMs, MaxIntervalMs);

    public static int ClampJitter(int value) => Math.Clamp(value, 0, MaxJitterMs);
}
