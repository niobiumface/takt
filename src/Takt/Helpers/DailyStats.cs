namespace Takt.Helpers;

public sealed class DayStat
{
    public string Date { get; set; } = "";
    public int Clicks { get; set; }
    public int Typed { get; set; }
    public int PresenceSeconds { get; set; }
}

public static class StatsBook
{
    public static void Add(IList<DayStat> days, DateTime localDay, int clicks, int typed, int presenceSeconds)
    {
        var key = localDay.ToString("yyyy-MM-dd");
        var day = days.FirstOrDefault(item => item.Date == key);
        if (day is null)
        {
            day = new DayStat { Date = key };
            days.Add(day);
        }

        day.Clicks += Math.Max(0, clicks);
        day.Typed += Math.Max(0, typed);
        day.PresenceSeconds += Math.Max(0, presenceSeconds);
    }

    public static IReadOnlyList<DayStat> LastDays(IEnumerable<DayStat> days, DateTime today, int count)
    {
        var map = days.ToDictionary(day => day.Date, StringComparer.Ordinal);
        var result = new List<DayStat>();
        for (var offset = count - 1; offset >= 0; offset--)
        {
            var key = today.Date.AddDays(-offset).ToString("yyyy-MM-dd");
            result.Add(map.TryGetValue(key, out var found) ? found : new DayStat { Date = key });
        }

        return result;
    }
}
