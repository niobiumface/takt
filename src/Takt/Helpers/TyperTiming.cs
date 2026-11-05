namespace Takt.Helpers;

public static class TyperTiming
{
    public static int DelayMs(int charactersPerMinute, int jitterPercent, Random rng)
    {
        var cpm = Math.Clamp(charactersPerMinute, SettingsLimits.MinCpm, SettingsLimits.MaxCpm);
        var baseMs = 60000d / cpm;
        var span = baseMs * (Math.Clamp(jitterPercent, 0, SettingsLimits.MaxJitterPercent) / 100d);
        var offset = span <= 0 ? 0 : ((rng.NextDouble() * 2d) - 1d) * span;
        return Math.Max(1, (int)Math.Round(baseMs + offset));
    }

    public static int WordsPerMinute(int charactersPerMinute) =>
        Math.Max(1, SettingsLimits.ClampCpm(charactersPerMinute) / 5);
}
