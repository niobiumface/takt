using Takt.Models;

namespace Takt.Helpers;

public static class ScheduleWindow
{
    public static bool IsInside(DateTime localNow, string start, string end)
    {
        if (!TimeSpan.TryParse(start, out var from))
            from = TimeSpan.Zero;
        if (!TimeSpan.TryParse(end, out var to))
            to = new TimeSpan(23, 59, 59);

        var now = localNow.TimeOfDay;
        if (from == to)
            return true;

        if (from < to)
            return now >= from && now < to;

        return now >= from || now < to;
    }

    public static bool IsInside(DateTime localNow, string start, string end, WeekdayMask days)
    {
        if (!IsInside(localNow, start, end))
            return false;
        if (days is WeekdayMask.None or WeekdayMask.All)
            return true;

        if (!TimeSpan.TryParse(start, out var from))
            from = TimeSpan.Zero;
        if (!TimeSpan.TryParse(end, out var to))
            to = new TimeSpan(23, 59, 59);

        var day = localNow.DayOfWeek;
        if (from > to && localNow.TimeOfDay < to)
            day = localNow.AddDays(-1).DayOfWeek;

        return days.HasFlag(ToFlag(day));
    }

    public static WeekdayMask ToFlag(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WeekdayMask.Monday,
        DayOfWeek.Tuesday => WeekdayMask.Tuesday,
        DayOfWeek.Wednesday => WeekdayMask.Wednesday,
        DayOfWeek.Thursday => WeekdayMask.Thursday,
        DayOfWeek.Friday => WeekdayMask.Friday,
        DayOfWeek.Saturday => WeekdayMask.Saturday,
        _ => WeekdayMask.Sunday
    };
}
