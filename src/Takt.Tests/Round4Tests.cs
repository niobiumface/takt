using System.IO;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;

namespace Takt.Tests;

public class CommandSearchTests
{
    [Fact]
    public void RanksExactMatchesAheadOfFuzzyOnes()
    {
        var entries = new CommandEntry[]
        {
            new("a", "Clicker interval", "ms", "clicker"),
            new("b", "Theme", "dark", "general")
        };
        var ranked = CommandSearch.Rank("click", entries);
        Assert.Equal("a", ranked[0].Id);
        Assert.Empty(CommandSearch.Rank("zzzz", entries));
    }

    [Fact]
    public void ReturnsEveryEntryWhenTheQueryIsEmpty()
    {
        var entries = new CommandEntry[] { new("a", "One", "", "general") };
        Assert.Single(CommandSearch.Rank("  ", entries));
    }
}

public class SettingsUndoStackTests
{
    [Fact]
    public void KeepsTheNewestSnapshotsInsideTheLimit()
    {
        var stack = new SettingsUndoStack(2);
        stack.Push("a");
        stack.Push("a");
        stack.Push("b");
        stack.Push("c");
        Assert.Equal(2, stack.Count);
        Assert.Equal("c", stack.Pop());
        Assert.Equal("b", stack.Pop());
        Assert.Null(stack.Pop());
    }
}

public class TyperTemplateTests
{
    [Fact]
    public void ExpandsDatetimeBeforeDateAndKeepsClipboard()
    {
        var when = new DateTime(2026, 11, 5, 9, 7, 0);
        var text = TyperTemplates.Expand("on {datetime} / {date} {time} [{clipboard}]", when, "note", "yyyy-MM-dd", "HH:mm", "yyyy-MM-dd HH:mm");
        Assert.Equal("on 2026-11-05 09:07 / 2026-11-05 09:07 [note]", text);
    }

    [Fact]
    public void FallsBackWhenTheFormatIsInvalid()
    {
        var when = new DateTime(2026, 1, 2);
        var text = TyperTemplates.Expand("{date}", when, null, "yyyy'", null, null);
        Assert.Equal("2026-01-02", text);
    }
}

public class RunDeadlineTests
{
    [Fact]
    public void StopsAfterMinutesAndAtAClockTimeThatWasStillAhead()
    {
        var start = new DateTime(2026, 11, 5, 17, 0, 0);
        Assert.False(RunDeadline.IsDue(start, start.AddMinutes(4), 5, ""));
        Assert.True(RunDeadline.IsDue(start, start.AddMinutes(5), 5, ""));
        Assert.True(RunDeadline.IsDue(start, new DateTime(2026, 11, 5, 18, 0, 0), 0, "18:00"));
        Assert.False(RunDeadline.IsDue(new DateTime(2026, 11, 5, 18, 5, 0), new DateTime(2026, 11, 5, 18, 6, 0), 0, "18:00"));
    }

    [Fact]
    public void ClockGateFiresOnceUntilTheRunStops()
    {
        var gate = new RunClockGate();
        var start = new DateTime(2026, 11, 5, 10, 0, 0);
        Assert.False(gate.Tick(true, start, 1, ""));
        Assert.True(gate.Tick(true, start.AddMinutes(1), 1, ""));
        Assert.False(gate.Tick(true, start.AddMinutes(2), 1, ""));
        Assert.False(gate.Tick(false, start.AddMinutes(3), 1, ""));
        var labeled = new RunClockGate();
        labeled.Tick(true, start, 1, "");
        Assert.Equal("00:01:00", labeled.Label(start, 1, ""));
    }
}

public class ProfileSwitchTests
{
    [Fact]
    public void MatchesTheProcessNameAfterTheDebounce()
    {
        var rules = new[] { ("WINWORD.exe", "Office", true), ("code", "Dev", false) };
        Assert.Equal("Office", AppProfileMatcher.Match(@"C:\Program Files\WINWORD.exe", rules));
        Assert.Null(AppProfileMatcher.Match("code", rules));

        var gate = new ProfileSwitchGate();
        var t0 = DateTime.UtcNow;
        Assert.Null(gate.Tick(true, "WINWORD", rules, t0, 800));
        Assert.Null(gate.Tick(true, "WINWORD", rules, t0.AddMilliseconds(100), 800));
        Assert.Equal("Office", gate.Tick(true, "WINWORD", rules, t0.AddMilliseconds(800), 800));
        Assert.Null(gate.Tick(true, "WINWORD", rules, t0.AddSeconds(2), 800));
        Assert.Null(gate.Tick(false, "WINWORD", rules, t0.AddSeconds(3), 800));
    }
}

public class StatsBookTests
{
    [Fact]
    public void AddsToTheLocalDayAndFillsTheWeek()
    {
        var days = new List<DayStat>();
        var today = new DateTime(2026, 11, 5);
        StatsBook.Add(days, today, 2, 3, 4);
        StatsBook.Add(days, today, 1, 0, 0);
        var week = StatsBook.LastDays(days, today, 7);
        Assert.Equal(7, week.Count);
        Assert.Equal(3, week[^1].Clicks);
        Assert.Equal(3, week[^1].Typed);
        Assert.Equal(4, week[^1].PresenceSeconds);
        Assert.Equal(today.AddDays(-6).ToString("yyyy-MM-dd"), week[0].Date);
    }
}

public class DiagnosisReportTests
{
    [Fact]
    public void MarksIdleHotkeysAndPolicy()
    {
        Assert.Equal(CheckMark.Pass, DiagnosisReport.Idle(true).Mark);
        Assert.Equal(CheckMark.Fail, DiagnosisReport.Idle(false).Mark);
        Assert.Equal(CheckMark.Warn, DiagnosisReport.Idle(null).Mark);
        Assert.Equal(CheckMark.Fail, DiagnosisReport.Hotkeys(1, 0).Mark);
        Assert.Equal(CheckMark.Warn, DiagnosisReport.LockPolicy(300).Mark);
        Assert.Contains("does not bypass", DiagnosisReport.LockPolicy(300).Explanation);
        Assert.Equal(CheckMark.Pass, DiagnosisReport.Screensaver(false).Mark);
    }
}

public class MacroAndSequenceTests
{
    [Fact]
    public void MovesItemsAndBuildsAWaitStep()
    {
        var items = new List<string> { "a", "b", "c" };
        Assert.True(MacroEdit.Move(items, 0, 1));
        Assert.Equal(["b", "a", "c"], items);
        Assert.False(MacroEdit.Move(items, 0, -1));
        var wait = MacroEdit.Wait(-5);
        Assert.True(MacroEdit.IsWait(wait.Kind));
        Assert.Equal(0, wait.DelayMs);
    }

    [Fact]
    public void UsesThePointButtonAndDelayWhenSet()
    {
        var point = new ClickPoint { CustomButton = true, Button = ClickButton.Right, DelayMs = 40 };
        var step = SequencePlan.For(point, ClickButton.Left);
        Assert.Equal(ClickButton.Right, step.Button);
        Assert.True(step.UsePointDelay);
        Assert.Equal(40, step.PointDelayMs);
        var plain = SequencePlan.For(new ClickPoint(), ClickButton.Middle);
        Assert.Equal(ClickButton.Middle, plain.Button);
        Assert.False(plain.UsePointDelay);
    }
}

public class SettingResetTests
{
    [Fact]
    public void RestoresIndividualValuesAndRejectsUnknownKeys()
    {
        var settings = new AppSettings();
        settings.Clicker.IntervalMs = 250;
        settings.Macro.LoopCount = 4;
        settings.General.StopAfterMinutes = 15;
        settings.Typer.DateFormat = "dd.MM.yyyy";
        Assert.True(SettingReset.Apply(settings, "interval"));
        Assert.True(SettingReset.Apply(settings, "loop"));
        Assert.True(SettingReset.Apply(settings, "stop-minutes"));
        Assert.True(SettingReset.Apply(settings, "date-format"));
        Assert.Equal(new AppSettings().Clicker.IntervalMs, settings.Clicker.IntervalMs);
        Assert.Equal(1, settings.Macro.LoopCount);
        Assert.Equal(0, settings.General.StopAfterMinutes);
        Assert.Equal("yyyy-MM-dd", settings.Typer.DateFormat);
        Assert.False(SettingReset.Apply(settings, "missing"));
    }
}

public class ClientAnimationTests
{
    [Fact]
    public void UsesZeroDurationWhenAnimationsAreOff()
    {
        Assert.Equal(TimeSpan.Zero, ClientAnimation.Fade(false, 160));
        Assert.Equal(TimeSpan.FromMilliseconds(160), ClientAnimation.Fade(true));
        Assert.True(ClientAnimation.ShouldAnimate(null));
        Assert.False(ClientAnimation.ShouldAnimate(false));
    }
}

public class ActivityToneTests
{
    [Fact]
    public void PrefersTyperThenClickerThenMotionThenPaused()
    {
        Assert.Equal(ActivityTone.Typer, ActivityTonePicker.Choose(true, true, true, true));
        Assert.Equal(ActivityTone.Clicker, ActivityTonePicker.Choose(false, true, true, true));
        Assert.Equal(ActivityTone.Motion, ActivityTonePicker.Choose(false, false, true, true));
        Assert.Equal(ActivityTone.Paused, ActivityTonePicker.Choose(false, false, false, true));
        Assert.Equal(ActivityTone.Idle, ActivityTonePicker.Choose(false, false, false, false));
        Assert.Equal("#3BA89A", ActivityTonePicker.Hex(ActivityTone.Motion, "#5B8DEF"));
    }
}

public class FieldFormatTests
{
    [Fact]
    public void AcceptsHexAndClockValues()
    {
        Assert.True(FieldFormat.IsHex("#5B8DEF"));
        Assert.True(FieldFormat.IsHex("5b8def"));
        Assert.False(FieldFormat.IsHex("blue"));
        Assert.False(FieldFormat.IsHex("#FFF"));
        Assert.True(FieldFormat.IsClock("08:00"));
        Assert.True(FieldFormat.IsClock("8:00"));
        Assert.False(FieldFormat.IsClock("25:00"));
        Assert.Equal("Left  ·  Single  ·  Infinite", ClickSummaryText.Format("Left", "Single", "Infinite"));
    }

    [Fact]
    public void ResetsRowFieldsWithoutDroppingTheRow()
    {
        var point = new ClickPoint { X = 12, Y = 40, DelayMs = 80, CustomButton = true, Button = ClickButton.Right };
        RowDefaults.Point(point);
        Assert.Equal(12, point.X);
        Assert.Equal(0, point.DelayMs);
        Assert.False(point.CustomButton);
        Assert.Equal(ClickButton.Left, point.Button);

        var window = new TimeSpanWindow { Start = "01:00", End = "02:00" };
        RowDefaults.Window(window);
        Assert.Equal("08:00", window.Start);
        Assert.Equal("17:00", window.End);

        var step = new MacroEvent { DelayMs = 400, X = 9 };
        RowDefaults.MacroDelay(step);
        Assert.Equal(0, step.DelayMs);
        Assert.Equal(9, step.X);

        var rule = new AppRule { Enabled = false, ProcessName = "code" };
        RowDefaults.Rule(rule);
        Assert.True(rule.Enabled);
        Assert.Equal("code", rule.ProcessName);
    }
}

public class PaceAndWeekTests
{
    [Fact]
    public void PresetsMatchOnlyTheExactValue()
    {
        Assert.True(PaceCatalog.Matches(100, 100));
        Assert.False(PaceCatalog.Matches(101, 100));
        Assert.Equal(4, PaceCatalog.Click.Length);
        Assert.Equal(3, PaceCatalog.Typer.Length);
    }

    [Fact]
    public void WeekStripUsesTheDayNumberAndKeepsTheDetail()
    {
        var days = StatsBook.LastDays(
            [new DayStat { Date = "2026-11-05", Clicks = 4, Typed = 2, PresenceSeconds = 9 }],
            new DateTime(2026, 11, 5),
            7);
        var cells = WeekStrip.Build(days);
        Assert.Equal(7, cells.Count);
        Assert.Equal("05", cells[^1].Day);
        Assert.Equal(4, cells[^1].Clicks);
        Assert.Equal("4/2/9s", cells[^1].Detail);
        Assert.Equal(0, cells[0].Clicks);
    }
}

public class SettingsUndoServiceTests
{
    [Fact]
    public void RestoresThePreviousFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-undo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        try
        {
            using var service = new SettingsService(path);
            service.Load();
            service.Current.Clicker.IntervalMs = 222;
            service.SaveNow();
            service.Current.Clicker.IntervalMs = 333;
            service.SaveNow();
            Assert.True(service.TryUndo());
            Assert.Equal(222, service.Current.Clicker.IntervalMs);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

public class StatsStoreTests
{
    [Fact]
    public void WritesAndResetsTheAppDataFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-stats-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "stats.json");
        try
        {
            var store = new StatsStore(path);
            store.Add(new DateTime(2026, 11, 5), 4, 5, 6);
            store.Flush();
            var again = new StatsStore(path);
            again.Load();
            Assert.Equal(4, again.Days.Single().Clicks);
            again.Reset();
            Assert.Empty(again.Days);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
