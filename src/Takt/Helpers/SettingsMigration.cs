using Takt.Models;

namespace Takt.Helpers;

public static class SettingsMigration
{
    public static bool Migrate(AppSettings settings)
    {
        if (settings.Version >= AppSettings.CurrentVersion)
            return false;

        settings.Work ??= new WorkScheduleSettings();
        if (settings.Presence is { Enabled: true, UseWorkHours: true })
        {
            settings.Work.Enabled = true;
            settings.Work.Outside = OutsideHoursBehavior.PauseAndResume;
            CopyDay(settings, DayOfWeek.Monday, settings.Presence.Monday);
            CopyDay(settings, DayOfWeek.Tuesday, settings.Presence.Tuesday);
            CopyDay(settings, DayOfWeek.Wednesday, settings.Presence.Wednesday);
            CopyDay(settings, DayOfWeek.Thursday, settings.Presence.Thursday);
            CopyDay(settings, DayOfWeek.Friday, settings.Presence.Friday);
            CopyDay(settings, DayOfWeek.Saturday, settings.Presence.Saturday);
            CopyDay(settings, DayOfWeek.Sunday, settings.Presence.Sunday);
        }
        else if (settings.Jiggler?.UseSchedule == true)
        {
            settings.Work.Enabled = true;
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
                CopyWindow(settings, day, settings.Jiggler.ScheduleStart, settings.Jiggler.ScheduleEnd);
        }

        if (settings.Clicker is not null && settings.Clicker.JitterPercent <= 0 && settings.Clicker.JitterMs > 0 && settings.Clicker.IntervalMs > 0)
            settings.Clicker.JitterPercent = Math.Clamp(settings.Clicker.JitterMs * 100 / settings.Clicker.IntervalMs, 0, 100);

        if (settings.General is not null)
            settings.General.CloseBehavior = settings.General.MinimizeToTray ? CloseBehavior.ToTray : CloseBehavior.Exit;

        settings.Version = AppSettings.CurrentVersion;
        return true;
    }

    private static void CopyDay(AppSettings settings, DayOfWeek day, bool include)
    {
        if (!include || settings.Presence is null)
            return;
        CopyWindow(settings, day, settings.Presence.ScheduleStart, settings.Presence.ScheduleEnd);
    }

    private static void CopyWindow(AppSettings settings, DayOfWeek day, string? start, string? end)
    {
        var match = settings.Work.Days.FirstOrDefault(item => item.Day == day);
        if (match is null)
        {
            match = new DayWindows { Day = day };
            settings.Work.Days.Add(match);
        }

        match.Windows ??= new();
        if (match.Windows.Count > 0)
            return;
        match.Windows.Add(new TimeSpanWindow
        {
            Start = string.IsNullOrWhiteSpace(start) ? "08:00" : start,
            End = string.IsNullOrWhiteSpace(end) ? "17:00" : end
        });
    }
}
