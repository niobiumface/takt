namespace Takt.Helpers;

public readonly record struct PacePreset(int Value, string Key, string Fallback);

public readonly record struct PaceChoice(int Value, string Label);

public static class PaceCatalog
{
    public static readonly PacePreset[] Click =
    [
        new(250, "Pace.Quiet", "Quiet"),
        new(100, "Pace.Normal", "Normal"),
        new(50, "Pace.Fast", "Fast"),
        new(20, "Pace.Rapid", "Rapid")
    ];

    public static readonly PacePreset[] Jiggle =
    [
        new(5_000, "Pace.J5", "5 s"),
        new(15_000, "Pace.J15", "15 s"),
        new(30_000, "Pace.J30", "30 s"),
        new(60_000, "Pace.J60", "60 s")
    ];

    public static readonly PacePreset[] Typer =
    [
        new(180, "Pace.SlowType", "Slow"),
        new(400, "Pace.NormalType", "Normal"),
        new(700, "Pace.FastType", "Fast")
    ];

    public static bool Matches(int current, int preset) => current == preset;
}

public readonly record struct WeekCell(string Day, int Clicks, string Detail);

public static class WeekStrip
{
    public static IReadOnlyList<WeekCell> Build(IReadOnlyList<DayStat> days) =>
        days.Select(day =>
        {
            var label = day.Date.Length >= 10 ? day.Date[8..] : day.Date;
            return new WeekCell(label, day.Clicks, $"{day.Clicks}/{day.Typed}/{day.PresenceSeconds}s");
        }).ToArray();
}
