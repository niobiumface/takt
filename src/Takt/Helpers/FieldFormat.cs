namespace Takt.Helpers;

public static class FieldFormat
{
    public static bool IsHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var text = value.Trim();
        if (text.StartsWith('#'))
            text = text[1..];
        if (text.Length is not (6 or 8))
            return false;
        foreach (var ch in text)
        {
            if (!Uri.IsHexDigit(ch))
                return false;
        }

        return true;
    }

    public static bool IsClock(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return TimeSpan.TryParseExact(
            value.Trim(),
            new[] { @"h\:mm", @"hh\:mm" },
            System.Globalization.CultureInfo.InvariantCulture,
            out _);
    }
}

public static class ClickSummaryText
{
    public static string Format(string button, string action, string repeat) =>
        button + "  ·  " + action + "  ·  " + repeat;
}

public static class RowDefaults
{
    public static void Point(Models.ClickPoint point)
    {
        point.DelayMs = 0;
        point.CustomButton = false;
        point.Button = Models.ClickButton.Left;
    }

    public static void Window(Models.TimeSpanWindow window)
    {
        window.Start = "08:00";
        window.End = "17:00";
    }

    public static void MacroDelay(Models.MacroEvent step) => step.DelayMs = 0;

    public static void Rule(Models.AppRule rule) => rule.Enabled = true;
}
