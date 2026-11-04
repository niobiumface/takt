namespace Takt.Helpers;

public static class TyperTemplates
{
    public static string Expand(string? text, DateTime localNow, string? clipboard, string? dateFormat, string? timeFormat, string? dateTimeFormat)
    {
        var source = text ?? "";
        var date = Safe(localNow, dateFormat, "yyyy-MM-dd");
        var time = Safe(localNow, timeFormat, "HH:mm");
        var both = Safe(localNow, dateTimeFormat, "yyyy-MM-dd HH:mm");
        return source
            .Replace("{datetime}", both, StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", date, StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", time, StringComparison.OrdinalIgnoreCase)
            .Replace("{clipboard}", clipboard ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static string Safe(DateTime value, string? format, string fallback)
    {
        try
        {
            return value.ToString(string.IsNullOrWhiteSpace(format) ? fallback : format);
        }
        catch (FormatException)
        {
            return value.ToString(fallback);
        }
    }
}
