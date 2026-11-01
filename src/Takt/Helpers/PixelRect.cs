namespace Takt.Helpers;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public static PixelRect FromScreen(VirtualScreen screen) =>
        new(screen.Left, screen.Top, screen.Left + screen.Width, screen.Top + screen.Height);
}
