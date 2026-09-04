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
}
