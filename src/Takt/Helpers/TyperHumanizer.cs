namespace Takt.Helpers;

public static class TyperHumanizer
{
    public static bool IsPunctuation(char value) => value is '.' or ',' or '!' or '?' or ';' or ':';

    public static int ExtraPauseMs(char value, int configuredMs) =>
        IsPunctuation(value) ? Math.Max(0, configuredMs) : 0;

    public static bool ShouldTypo(char value, int frequencyPercent, Random rng)
    {
        if (!char.IsLetter(value) || frequencyPercent <= 0)
            return false;
        return rng.Next(100) < Math.Clamp(frequencyPercent, 0, 100);
    }

    public static char Mistype(char value, Random rng)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var lower = char.ToLowerInvariant(value);
        var index = letters.IndexOf(lower);
        if (index < 0)
            return value;
        var step = rng.Next(2) == 0 ? -1 : 1;
        var next = letters[(index + step + letters.Length) % letters.Length];
        return char.IsUpper(value) ? char.ToUpperInvariant(next) : next;
    }
}
