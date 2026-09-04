namespace Takt.Helpers;

public static class RadiusArea
{
    public static bool Contains(int centerX, int centerY, int x, int y, int radius)
    {
        var r = Math.Max(radius, 1);
        var dx = (long)x - centerX;
        var dy = (long)y - centerY;
        return dx * dx + dy * dy <= (long)r * r;
    }

    public static (int X, int Y) Clamp(int centerX, int centerY, int x, int y, int radius)
    {
        var r = Math.Max(radius, 1);
        var dx = (double)x - centerX;
        var dy = (double)y - centerY;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist <= r || dist < 0.0001)
            return (x, y);

        var scale = r / dist;
        return (
            centerX + (int)Math.Round(dx * scale),
            centerY + (int)Math.Round(dy * scale));
    }

    public static (int X, int Y) PointOnCircle(int centerX, int centerY, int radius, double phase)
    {
        var r = Math.Max(radius, 1);
        return (
            centerX + (int)Math.Round(Math.Cos(phase) * r),
            centerY + (int)Math.Round(Math.Sin(phase) * r));
    }

    public static (int X, int Y) RandomInCircle(int centerX, int centerY, int radius, Random rng)
    {
        var r = Math.Max(radius, 1);
        var angle = rng.NextDouble() * Math.PI * 2;
        var dist = Math.Sqrt(rng.NextDouble()) * r;
        return (
            centerX + (int)Math.Round(Math.Cos(angle) * dist),
            centerY + (int)Math.Round(Math.Sin(angle) * dist));
    }
}
