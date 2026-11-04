namespace Takt.Helpers;

public static class ClientAnimation
{
    public static bool ShouldAnimate(bool? systemEnabled) => systemEnabled != false;

    public static TimeSpan Fade(bool animate, int milliseconds = 160) =>
        animate ? TimeSpan.FromMilliseconds(Math.Max(0, milliseconds)) : TimeSpan.Zero;
}
