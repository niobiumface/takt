namespace Takt.Helpers;

public static class FocusGate
{
    public static bool Allows(string? wantedProcess, string? foregroundPath)
    {
        if (string.IsNullOrWhiteSpace(wantedProcess))
            return true;
        var wanted = AnchorNames.Process(wantedProcess);
        if (wanted.Length == 0)
            return true;
        var actual = AnchorNames.Process(foregroundPath);
        return string.Equals(wanted, actual, StringComparison.OrdinalIgnoreCase);
    }
}
