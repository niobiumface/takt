namespace Takt.Helpers;

public static class AwakePolicy
{
    public static bool ShouldRequest(bool enabled, bool paused, bool preventSleep, bool preventDisplay) =>
        enabled && !paused && (preventSleep || preventDisplay);
}
