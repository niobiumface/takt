namespace Takt.Helpers;

public static class SettingsLimits
{
    public const int MinAmplitude = 1;
    public const int MaxAmplitude = 50;
    public const int MinRadius = 8;
    public const int MaxRadius = 4000;
    public const int MinFailsafe = 4;
    public const int MaxFailsafe = 64;
    public const int MinIntervalMs = 1;
    public const int MaxIntervalMs = 3_600_000;
    public const int MinJitterMs = 0;
    public const int MaxJitterMs = 60_000;
    public const int MinIdleSeconds = 1;
    public const int MaxIdleSeconds = 86_400;
    public const int MinBurst = 0;
    public const int MaxBurst = 100_000;
    public const int MinRepeatCount = 1;
    public const int MaxRepeatCount = 10_000_000;
    public const int MinDurationSeconds = 1;
    public const int MaxDurationSeconds = 86_400;
    public const int MinStartDelayMs = 0;
    public const int MaxStartDelayMs = 3_600_000;
    public const int MinCpm = 10;
    public const int MaxCpm = 2_000;
    public const int MinJitterPercent = 0;
    public const int MaxJitterPercent = 80;

    public static int ClampAmplitude(int value) => Math.Clamp(value, MinAmplitude, MaxAmplitude);
    public static int ClampRadius(int value) => Math.Clamp(value, MinRadius, MaxRadius);
    public static int ClampFailsafe(int value) => Math.Clamp(value, MinFailsafe, MaxFailsafe);
    public static int ClampInterval(int value) => Math.Clamp(value, MinIntervalMs, MaxIntervalMs);
    public static int ClampJitter(int value) => Math.Clamp(value, MinJitterMs, MaxJitterMs);
    public static int ClampIdleSeconds(int value) => Math.Clamp(value, MinIdleSeconds, MaxIdleSeconds);
    public static int ClampBurst(int value) => Math.Clamp(value, MinBurst, MaxBurst);
    public static int ClampRepeatCount(int value) => Math.Clamp(value, MinRepeatCount, MaxRepeatCount);
    public static int ClampDuration(int value) => Math.Clamp(value, MinDurationSeconds, MaxDurationSeconds);
    public static int ClampStartDelay(int value) => Math.Clamp(value, MinStartDelayMs, MaxStartDelayMs);
    public static int ClampCpm(int value) => Math.Clamp(value, MinCpm, MaxCpm);
    public static int ClampJitterPercent(int value) => Math.Clamp(value, MinJitterPercent, MaxJitterPercent);

    public static string ClampSchedule(string? value, string fallback)
    {
        if (!TimeSpan.TryParse(value, out var time))
            return fallback;
        if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            return fallback;
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}";
    }
}
