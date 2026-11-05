using System.IO;
using Takt.Helpers;
using Takt.Models;
using Takt.Native;
using Takt.Services;

namespace Takt.Tests;

public class PauseResumeTests
{
    [Fact]
    public void SuspendRemembersWhatIsRunningAndResumeClearsIt()
    {
        var running = PauseResume.Capture(jiggler: true, clicker: true, typer: false, presence: true, macroPlaying: false);
        Assert.Equal(PauseAction.Suspended, PauseResume.Decide(running, EngineFlags.None));
        Assert.Equal(PauseAction.Resumed, PauseResume.Decide(EngineFlags.None, running));
        Assert.Equal(PauseAction.Idle, PauseResume.Decide(EngineFlags.None, EngineFlags.None));
        Assert.True(running.HasFlag(EngineFlags.Jiggler));
        Assert.False(running.HasFlag(EngineFlags.Typer));
        Assert.False(running.HasFlag(EngineFlags.Macro));
    }
}

public class MacroAnchorTests
{
    [Fact]
    public void OffsetFollowsTheWindowAndFallsBackWhenItIsGone()
    {
        var offset = MacroAnchor.Offset(120, 80, 100, 40);
        Assert.Equal((20, 40), offset);
        var restored = MacroAnchor.Place(offset.RelX, offset.RelY, 300, 10);
        Assert.Equal((320, 50), restored);

        var anchored = new MacroEvent
        {
            X = 120,
            Y = 80,
            Anchored = true,
            RelX = 20,
            RelY = 40,
            AnchorProcess = "notepad",
            AnchorTitle = "Notes"
        };
        var found = MacroAnchor.Resolve(anchored, (process, title) =>
            process == "notepad" && title == "Notes" ? new AnchorSnapshot(300, 10, process, title) : null);
        Assert.Equal((320, 50), found);
        var missing = MacroAnchor.Resolve(anchored, (_, _) => null);
        Assert.Equal((120, 80), missing);
        Assert.Equal("notepad", AnchorNames.Process(@"C:\Windows\notepad.exe"));
    }

    [Fact]
    public void SpeedScalesDelaysAndThePathSkipsText()
    {
        Assert.Equal(200, MacroPace.PlaybackDelay(100, "move", MacroSpeed.Half));
        Assert.Equal(50, MacroPace.PlaybackDelay(100, "move", MacroSpeed.Double));
        Assert.Equal(2_000, MacroPace.PlaybackDelay(9_000, "move", MacroSpeed.Normal));
        Assert.Equal(4_000, MacroPace.PlaybackDelay(9_000, "move", MacroSpeed.Half));
        var dots = MacroPath.Dots(
        [
            new MacroEvent { Kind = "move", X = 0, Y = 0 },
            new MacroEvent { Kind = "text", Text = "hi", X = 500, Y = 500 },
            new MacroEvent { Kind = "move", Anchored = true, RelX = 10, RelY = 20, X = 1, Y = 1 }
        ], 106, 106);
        Assert.Equal(2, dots.Count);
        Assert.Equal(6, dots[0].X, 0);
        Assert.Equal(100, dots[1].X, 0);
    }

    [Fact]
    public void TextStepsTypeNewlinesOnceAndRecordingStaysPointerOnly()
    {
        Assert.Equal(new[] { 'A', '\r', 'B' }, MacroText.Keys("A\r\nB").ToArray());
        Assert.Empty(MacroText.Keys(""));
        Assert.True(MacroEdit.IsText(MacroEdit.Text("hello").Kind));
        Assert.Equal("move", MacroEngine.PointerKind(Win32.WmMouseMove));
        Assert.Equal("down", MacroEngine.PointerKind(Win32.WmLButtonDown));
        Assert.Equal("up", MacroEngine.PointerKind(Win32.WmLButtonUp));
        Assert.Equal("wheel", MacroEngine.PointerKind(Win32.WmMouseWheel));
        Assert.False(MacroEdit.IsText(MacroEngine.PointerKind(Win32.WmMouseMove)));
    }

    [Fact]
    public void NamedMacrosAndSequencesCopyByValue()
    {
        var live = new MacroSettings
        {
            LoopCount = 3,
            Speed = MacroSpeed.Double,
            Events = [new MacroEvent { Kind = "text", Text = "hi", X = 4 }]
        };
        var library = new List<SavedMacro>();
        SavedLibraries.StoreMacro("Alpha", live, library);
        live.Events[0].Text = "changed";
        Assert.Equal("hi", library[0].Events[0].Text);
        SavedLibraries.StoreMacro("alpha", live, library);
        Assert.Single(library);
        var target = new MacroSettings();
        SavedLibraries.LoadMacro(library[0], target);
        Assert.Equal(MacroSpeed.Double, target.Speed);
        Assert.Equal("changed", target.Events[0].Text);

        var points = new List<ClickPoint> { new() { X = 8, Y = 9, DelayMs = 5 } };
        var sequences = new List<SavedSequence>();
        SavedLibraries.StoreSequence(" Row ", points, sequences);
        points[0].X = 1;
        Assert.Equal(8, sequences[0].Points[0].X);
        var loaded = new List<ClickPoint>();
        SavedLibraries.LoadSequence(sequences[0], loaded);
        Assert.Equal(8, loaded[0].X);
    }
}

public class RemainderAndSettingsFileTests
{
    [Fact]
    public void RemainderCountsDownOnlyForCountAndDuration()
    {
        Assert.Equal(40, RunRemainder.CountLeft(RepeatMode.Count, 60, 100));
        Assert.Null(RunRemainder.CountLeft(RepeatMode.Infinite, 60, 100));
        var started = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        var now = started.AddSeconds(15);
        Assert.Equal(45, RunRemainder.SecondsLeft(RepeatMode.Duration, started, now, 60));
        Assert.Null(RunRemainder.SecondsLeft(RepeatMode.Count, started, now, 60));
        Assert.Null(RunRemainder.SecondsLeft(RepeatMode.Duration, null, now, 60));
    }

    [Fact]
    public void ImportKeepsProfilesOnlyWhenTheFileHasThem()
    {
        Assert.True(SettingsImport.ContainsProfiles("{\"profiles\":[]}"));
        Assert.False(SettingsImport.ContainsProfiles("{\"jiggler\":{}}"));
        var from = new AppSettings();
        from.Profiles.Add(new ActivityProfile { Name = "Office" });
        from.Profiles[0].Clicker.IntervalMs = 250;
        var to = new AppSettings();
        to.Profiles.Add(new ActivityProfile { Name = "Keep" });
        ProfileApplicator.ReplaceProfiles(from, to);
        Assert.Equal("Office", to.Profiles[0].Name);
        Assert.Equal(250, to.Profiles[0].Clicker.IntervalMs);
        from.Profiles[0].Clicker.IntervalMs = 10;
        Assert.Equal(250, to.Profiles[0].Clicker.IntervalMs);
    }

    [Fact]
    public void BackupRotatesThreeCopiesAndCsvStaysLocal()
    {
        var folder = Path.Combine(Path.GetTempPath(), "takt-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "settings.json");
            File.WriteAllText(path, "one");
            SettingsBackup.Rotate(path);
            File.WriteAllText(path, "two");
            SettingsBackup.Rotate(path);
            File.WriteAllText(path, "three");
            SettingsBackup.Rotate(path);
            File.WriteAllText(path, "four");
            Assert.Equal("four", File.ReadAllText(path));
            Assert.Equal("three", File.ReadAllText(SettingsBackup.Name(path, 1)));
            Assert.Equal("two", File.ReadAllText(SettingsBackup.Name(path, 2)));
            Assert.Equal("one", File.ReadAllText(SettingsBackup.Name(path, 3)));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }

        var csv = StatsCsv.Format(
        [
            new DayStat { Date = "2026-11-05", Clicks = 2, Typed = 3, PresenceSeconds = 4 },
            new DayStat { Date = "2026-11-04", Clicks = 1, Typed = 0, PresenceSeconds = 0 }
        ]);
        Assert.StartsWith("date,clicks,typed,presenceSeconds\r\n2026-11-04,1,0,0\r\n", csv);
        Assert.Contains("2026-11-05,2,3,4", csv);
    }
}
