using Takt.Models;

namespace Takt.Helpers;

public static class FailsafeRegion
{
    public static bool IsInCorner(
        int x,
        int y,
        VirtualScreen screen,
        FailsafeCorner corner,
        int size)
    {
        return IsInCorner(x, y, PixelRect.FromScreen(screen), corner, size);
    }

    public static bool IsInCorner(int x, int y, PixelRect monitor, FailsafeCorner corner, int size)
    {
        var edge = Math.Max(size, 1);
        var left = monitor.Left;
        var top = monitor.Top;
        var right = monitor.Right;
        var bottom = monitor.Bottom;
        if (x < left || y < top || x >= right || y >= bottom)
            return false;

        return corner switch
        {
            FailsafeCorner.TopLeft => x <= left + edge && y <= top + edge,
            FailsafeCorner.TopRight => x >= right - edge && y <= top + edge,
            FailsafeCorner.BottomLeft => x <= left + edge && y >= bottom - edge,
            FailsafeCorner.BottomRight => x >= right - edge && y >= bottom - edge,
            _ => false
        };
    }
}
