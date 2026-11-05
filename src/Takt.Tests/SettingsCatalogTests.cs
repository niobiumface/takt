using System.IO;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;

namespace Takt.Tests;

public class SettingsMigrationTests
{
    [Fact]
    public void CarriesVersion1PresenceHoursIntoTheWeeklySchedule()
    {
        var settings = new AppSettings
        {
            Version = 1,
            Presence = new PresenceSettings
            {
                Enabled = true,
                UseWorkHours = true,
                Monday = true,
                Friday = true,
                ScheduleStart = "08:00",
                ScheduleEnd = "12:00"
            },
            Clicker = new ClickerSettings { IntervalMs = 200, JitterMs = 50 }
        };

        Assert.True(SettingsMigration.Migrate(settings));
        Assert.Equal(2, settings.Version);
        Assert.True(settings.Work.Enabled);
        Assert.Equal(25, settings.Clicker.JitterPercent);
        var monday = settings.Work.Days.Single(day => day.Day == DayOfWeek.Monday);
        Assert.Equal("08:00", monday.Windows[0].Start);
        Assert.Empty(settings.Work.Days.Single(day => day.Day == DayOfWeek.Saturday).Windows);
    }

    [Fact]
    public void LoadsAVersion1FileWithoutDroppingValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-migrate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        try
        {
            File.WriteAllText(path, """
            {
              "version": 1,
              "jiggler": { "amplitude": 11, "useSchedule": true, "scheduleStart": "09:00", "scheduleEnd": "18:00" }
            }
            """);
            using var service = new SettingsService(path);
            service.Load();
            Assert.Equal(2, service.Current.Version);
            Assert.Equal(11, service.Current.Jiggler.Amplitude);
            Assert.True(service.Current.Work.Enabled);
            Assert.NotEmpty(service.Current.Work.Days.Single(day => day.Day == DayOfWeek.Wednesday).Windows);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class WorkScheduleTests
{
    [Fact]
    public void TreatsLunchAsAGapBetweenWindows()
    {
        var schedule = ScheduleFor(DayOfWeek.Thursday, ("08:00", "12:00"), ("13:00", "17:00"));
        Assert.Equal(ScheduleEffect.Run, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 3, 9, 0, 0), schedule));
        Assert.Equal(ScheduleEffect.Pause, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 3, 12, 30, 0), schedule));
        Assert.Equal(ScheduleEffect.Run, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 3, 15, 0, 0), schedule));
    }

    [Fact]
    public void HolidaysAndOvernightWindowsFollowTheChosenBehavior()
    {
        var schedule = ScheduleFor(DayOfWeek.Friday, ("22:00", "06:00"));
        schedule.Holidays.Add("2026-09-03");
        Assert.Equal(ScheduleEffect.Pause, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 3, 10, 0, 0), schedule));
        Assert.Equal(ScheduleEffect.Run, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 4, 23, 0, 0), schedule));
        Assert.Equal(ScheduleEffect.Run, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 5, 5, 0, 0), schedule));
        schedule.Outside = OutsideHoursBehavior.Stop;
        Assert.Equal(ScheduleEffect.Stop, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 5, 12, 0, 0), schedule));
        schedule.Outside = OutsideHoursBehavior.HintOnly;
        Assert.Equal(ScheduleEffect.Hint, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 5, 12, 0, 0), schedule));
        schedule.Enabled = false;
        Assert.Equal(ScheduleEffect.Run, WorkScheduleEvaluator.Evaluate(new DateTime(2026, 9, 5, 12, 0, 0), schedule));
    }

    private static WorkScheduleSettings ScheduleFor(DayOfWeek day, params (string Start, string End)[] windows)
    {
        var schedule = new WorkScheduleSettings { Enabled = true };
        var match = schedule.Days.Single(item => item.Day == day);
        foreach (var window in windows)
            match.Windows.Add(new TimeSpanWindow { Start = window.Start, End = window.End });
        return schedule;
    }
}

public class HotkeyConflictTests
{
    [Fact]
    public void ReportsBothActionsWhenTheShortcutIsShared()
    {
        var duplicates = HotkeyConflicts.DuplicateIds(
        [
            ("ToggleClicker", 0, 0x76),
            ("ToggleJiggler", 0, 0x76),
            ("Panic", 0, 0x78),
            ("Cleared", 0, 0)
        ]);
        Assert.Equal(["ToggleClicker", "ToggleJiggler"], duplicates);
    }
}

public class TyperHumanizerTests
{
    [Fact]
    public void AddsPunctuationPausesAndDeterministicTypos()
    {
        Assert.Equal(120, TyperHumanizer.ExtraPauseMs('.', 120));
        Assert.Equal(0, TyperHumanizer.ExtraPauseMs('a', 120));
        var rng = new Random(2);
        Assert.False(TyperHumanizer.ShouldTypo('.', 100, rng));
        Assert.True(TyperHumanizer.ShouldTypo('a', 100, new Random(1)));
        var typo = TyperHumanizer.Mistype('b', new Random(3));
        Assert.True(typo is 'a' or 'c');
        Assert.Equal(typo, TyperHumanizer.Mistype('b', new Random(3)));
    }
}

public class ClickerTimingTests
{
    [Fact]
    public void PrefersPercentJitterAndKeepsMillisecondJitter()
    {
        Assert.Equal(20, ClickerTiming.JitterMilliseconds(200, 5, 10));
        Assert.Equal(5, ClickerTiming.JitterMilliseconds(200, 5, 0));
    }
}
