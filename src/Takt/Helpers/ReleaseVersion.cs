namespace Takt.Helpers;

public static class ReleaseVersion
{
    public static bool IsNewer(string? tag, Version current)
    {
        if (string.IsNullOrWhiteSpace(tag) || current is null)
            return false;
        var text = tag.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
            text = text[1..];
        var dash = text.IndexOf('-');
        if (dash >= 0)
            text = text[..dash];
        return Version.TryParse(text, out var latest) && latest > current;
    }
}
