namespace Takt.Helpers;

public static class LogTail
{
    public static string LastLines(string? text, int maxLines)
    {
        if (maxLines < 1 || string.IsNullOrEmpty(text))
            return "";
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length > 0 && lines[^1].Length == 0)
            lines = lines[..^1];
        var start = Math.Max(0, lines.Length - maxLines);
        return string.Join(Environment.NewLine, lines.Skip(start));
    }
}
