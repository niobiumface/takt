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
        var edge = Math.Max(size, 1);
        var left = screen.Left;
        var top = screen.Top;
        var right = screen.Left + screen.Width;
        var bottom = screen.Top + screen.Height;

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
