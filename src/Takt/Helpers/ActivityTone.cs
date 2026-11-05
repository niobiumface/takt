namespace Takt.Helpers;

public enum ActivityTone
{
    Idle,
    Paused,
    Clicker,
    Motion,
    Typer
}

public static class ActivityTonePicker
{
    public static ActivityTone Choose(bool typerRunning, bool clickerRunning, bool motionRunning, bool anyPaused)
    {
        if (typerRunning)
            return ActivityTone.Typer;
        if (clickerRunning)
            return ActivityTone.Clicker;
        if (motionRunning)
            return ActivityTone.Motion;
        if (anyPaused)
            return ActivityTone.Paused;
        return ActivityTone.Idle;
    }

    public static string Hex(ActivityTone tone, string? accentHex) => tone switch
    {
        ActivityTone.Typer => "#8B7CF6",
        ActivityTone.Clicker => string.IsNullOrWhiteSpace(accentHex) ? "#5B8DEF" : accentHex,
        ActivityTone.Motion => "#3BA89A",
        ActivityTone.Paused => "#E0B25D",
        _ => "#78808C"
    };
}
