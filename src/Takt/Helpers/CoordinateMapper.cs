namespace Takt.Helpers;

public readonly record struct VirtualScreen(int Left, int Top, int Width, int Height);

public readonly record struct MappedPoint(int X, int Y);

public static class CoordinateMapper
{
    public static MappedPoint ToAbsolute65535(int pixelX, int pixelY, VirtualScreen screen)
    {
        var width = Math.Max(screen.Width - 1, 1);
        var height = Math.Max(screen.Height - 1, 1);
        var x = (int)Math.Round((pixelX - screen.Left) * 65535d / width);
        var y = (int)Math.Round((pixelY - screen.Top) * 65535d / height);
        return new MappedPoint(Clamp(x), Clamp(y));
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 65535);
}
