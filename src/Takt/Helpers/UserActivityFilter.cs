namespace Takt.Helpers;

public static class UserActivityFilter
{
    public const int MoveThresholdPx = 10;

    public static bool IsInjected(uint flags, bool keyboard) =>
        keyboard
            ? (flags & 0x00000010) != 0
            : (flags & 0x00000003) != 0;

    public static bool IsButtonOrWheel(int mouseMessage) => mouseMessage is
        0x0201 or 0x0202 or // L down/up
        0x0204 or 0x0205 or // R
        0x0207 or 0x0208 or // M
        0x020A or 0x020E or // wheel / hwheel
        0x020B or 0x020C;   // X

    public static bool IsMouseMove(int mouseMessage) => mouseMessage == 0x0200;

    public static bool IsSignificantMove(int fromX, int fromY, int toX, int toY, int threshold = MoveThresholdPx)
    {
        var dx = toX - fromX;
        var dy = toY - fromY;
        return (long)dx * dx + (long)dy * dy >= (long)threshold * threshold;
    }
}
