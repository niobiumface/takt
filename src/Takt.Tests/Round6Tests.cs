using Takt.Helpers;
using Takt.Models;

namespace Takt.Tests;

public class LaterRoundTests
{
    [Fact]
    public void YearlyHolidayMatchesTheSameDayEveryYear()
    {
        var christmas = HolidayCalendar.Yearly(12, 25);
        Assert.True(HolidayCalendar.Matches(new DateTime(2026, 12, 25), christmas));
        Assert.True(HolidayCalendar.Matches(new DateTime(2027, 12, 25), christmas));
        Assert.False(HolidayCalendar.Matches(new DateTime(2026, 12, 24), christmas));
        Assert.True(HolidayCalendar.Matches(new DateTime(2026, 12, 25), "2026-12-25"));
        Assert.False(HolidayCalendar.Matches(new DateTime(2027, 12, 25), "2026-12-25"));
        Assert.Equal("25.12. every year", HolidayCalendar.Label(christmas, "every year"));
        Assert.True(WorkScheduleEvaluator.IsHoliday(new DateTime(2028, 12, 25), [christmas]));
    }

    [Fact]
    public void FocusGateUsesTheProcessName()
    {
        Assert.True(FocusGate.Allows("", @"C:\Apps\notepad.exe"));
        Assert.True(FocusGate.Allows("notepad", @"C:\Apps\notepad.exe"));
        Assert.True(FocusGate.Allows(@"D:\notepad.exe", "notepad"));
        Assert.False(FocusGate.Allows("winword", @"C:\Apps\notepad.exe"));
    }

    [Fact]
    public void PlanAdvancesOnceAndStopsOnTheLastStep()
    {
        var cursor = new PlanCursor();
        var start = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        Assert.True(cursor.Start(2, start));
        Assert.False(cursor.TryTimeout(start.AddSeconds(9), 10, 2, out _));
        Assert.True(cursor.TryTimeout(start.AddSeconds(10), 10, 2, out var hasNext));
        Assert.True(hasNext);
        Assert.Equal(1, cursor.Index);
        var ticket = cursor.Ticket;
        Assert.False(cursor.Leave(ticket - 1, 2, start.AddSeconds(11), out _));
        Assert.True(cursor.Leave(ticket, 2, start.AddSeconds(11), out hasNext));
        Assert.False(hasNext);
        Assert.False(cursor.IsActive);
        Assert.Equal(1, PlanLimits.Seconds(PlanKind.Jiggler, 0));
        Assert.Equal(0, PlanLimits.Seconds(PlanKind.Snippet, 0));
        var sequences = new[] { new SavedSequence { Name = "Row" } };
        Assert.Equal("Row", PlanLibrary.FindSequence(sequences, " row ")?.Name);
        Assert.Null(PlanLibrary.FindSnippet([], "missing"));
    }

    [Fact]
    public void LogTailKeepsTheLastLines()
    {
        var text = "one\r\ntwo\r\nthree\r\n";
        var tail = LogTail.LastLines(text, 2);
        Assert.Contains("two", tail);
        Assert.Contains("three", tail);
        Assert.DoesNotContain("one", tail);
        Assert.Equal("", LogTail.LastLines("", 4));
    }
}
