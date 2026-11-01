using Takt.Models;

namespace Takt.Helpers;

public static class WorkScheduleEvaluator
{
    public static ScheduleEffect Evaluate(DateTime localNow, WorkScheduleSettings? schedule)
    {
        if (schedule is null || !schedule.Enabled)
            return ScheduleEffect.Run;
        if (IsHoliday(localNow, schedule.Holidays) || !IsInside(localNow, schedule.Days))
            return Map(schedule.Outside);
        return ScheduleEffect.Run;
    }

    public static bool IsHoliday(DateTime localNow, IEnumerable<string>? holidays)
    {
        if (holidays is null)
            return false;
        var key = localNow.ToString("yyyy-MM-dd");
        foreach (var holiday in holidays)
        {
            if (string.Equals(holiday?.Trim(), key, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool IsInside(DateTime localNow, IEnumerable<DayWindows>? days)
    {
        if (days is null)
            return false;
        foreach (var day in days)
        {
            if (day.Windows is null)
                continue;
            foreach (var window in day.Windows)
            {
                if (Covers(localNow, day.Day, window.Start, window.End))
                    return true;
            }
        }

        return false;
    }

    public static bool Covers(DateTime localNow, DayOfWeek owner, string? start, string? end)
    {
        if (!TimeSpan.TryParse(start, out var from) || !TimeSpan.TryParse(end, out var to))
            return false;
        var time = localNow.TimeOfDay;
        if (from == to)
            return localNow.DayOfWeek == owner;
        if (from < to)
            return localNow.DayOfWeek == owner && time >= from && time < to;
        if (localNow.DayOfWeek == owner && time >= from)
            return true;
        return localNow.DayOfWeek == Next(owner) && time < to;
    }

    public static ScheduleEffect Map(OutsideHoursBehavior behavior) => behavior switch
    {
        OutsideHoursBehavior.Stop => ScheduleEffect.Stop,
        OutsideHoursBehavior.HintOnly => ScheduleEffect.Hint,
        _ => ScheduleEffect.Pause
    };

    private static DayOfWeek Next(DayOfWeek day) => day == DayOfWeek.Saturday ? DayOfWeek.Sunday : day + 1;
}
