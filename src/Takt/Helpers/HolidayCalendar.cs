namespace Takt.Helpers;

public static class HolidayCalendar
{
    public const string YearlyPrefix = "every:";

    public static string Exact(DateTime date) => date.ToString("yyyy-MM-dd");

    public static string Yearly(int month, int day) => $"{YearlyPrefix}{month:00}-{day:00}";

    public static bool IsYearly(string? stored) =>
        stored?.Trim().StartsWith(YearlyPrefix, StringComparison.OrdinalIgnoreCase) == true;

    public static bool Matches(DateTime local, string? stored)
    {
        var text = stored?.Trim() ?? "";
        if (text.Length == 0)
            return false;
        if (IsYearly(text))
        {
            if (!TryMonthDay(text[YearlyPrefix.Length..], out var month, out var day))
                return false;
            return local.Month == month && local.Day == day;
        }

        return string.Equals(text, local.ToString("yyyy-MM-dd"), StringComparison.Ordinal);
    }

    public static string Label(string? stored, string yearlyWord)
    {
        var text = stored?.Trim() ?? "";
        if (!IsYearly(text) || !TryMonthDay(text[YearlyPrefix.Length..], out var month, out var day))
            return text;
        return $"{day:00}.{month:00}. {yearlyWord}";
    }

    private static bool TryMonthDay(string body, out int month, out int day)
    {
        month = 0;
        day = 0;
        var parts = body.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out month) || !int.TryParse(parts[1], out day))
            return false;
        return month is >= 1 and <= 12 && day is >= 1 and <= 31;
    }
}
