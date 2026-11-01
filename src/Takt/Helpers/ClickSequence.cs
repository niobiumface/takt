namespace Takt.Helpers;

public static class ClickSequence
{
    public static int NextIndex(int current, int count)
    {
        if (count <= 0)
            return 0;
        var index = current % count;
        return (index + 1) % count;
    }
}
