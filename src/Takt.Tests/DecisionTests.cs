using System.IO;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;

namespace Takt.Tests;

public class AwakePolicyTests
{
    [Fact]
    public void RequestsAwakeOnlyWhileRunningAndNotPaused()
    {
        Assert.True(AwakePolicy.ShouldRequest(enabled: true, paused: false, preventSleep: true, preventDisplay: false));
        Assert.True(AwakePolicy.ShouldRequest(true, false, false, true));
        Assert.False(AwakePolicy.ShouldRequest(true, true, true, true));
        Assert.False(AwakePolicy.ShouldRequest(false, false, true, true));
        Assert.False(AwakePolicy.ShouldRequest(true, false, false, false));
    }
}

public class HoldPlanTests
{
    [Fact]
    public void CountModeEndsInsteadOfWaitingForever()
    {
        var plan = HoldPlan.For(RepeatMode.Count, repeatCount: 4, durationSeconds: 60, intervalMs: 25);
        Assert.False(plan.UntilCancelled);
        Assert.Equal(4, plan.Cycles);
        Assert.Equal(TimeSpan.FromMilliseconds(25), plan.ActiveHold);
    }

    [Fact]
    public void InfiniteModeWaitsUntilCancelled()
    {
        var plan = HoldPlan.For(RepeatMode.Infinite, 3, 10, 100);
        Assert.True(plan.UntilCancelled);
        Assert.Equal(Timeout.InfiniteTimeSpan, plan.ActiveHold);
    }

    [Fact]
    public void DurationModeUsesSecondsAndAtLeastOne()
    {
        var plan = HoldPlan.For(RepeatMode.Duration, 9, 0, 100);
        Assert.False(plan.UntilCancelled);
        Assert.Equal(1, plan.Cycles);
        Assert.Equal(TimeSpan.FromSeconds(1), plan.ActiveHold);
    }
}

public class HoldSessionTests
{
    [Fact]
    public void ReleasesTheButtonThatWasPressed()
    {
        var session = new HoldSession();
        session.Press(ClickButton.Left);
        Assert.Equal(ClickButton.Left, session.Release());
        Assert.Null(session.Release());
    }
}

public class SettingsLimitTests
{
    [Fact]
    public void ClampsEngineBounds()
    {
        Assert.Equal(50, SettingsLimits.ClampAmplitude(500));
        Assert.Equal(1, SettingsLimits.ClampAmplitude(0));
        Assert.Equal(8, SettingsLimits.ClampRadius(1));
        Assert.Equal(4000, SettingsLimits.ClampRadius(9000));
        Assert.Equal(4, SettingsLimits.ClampFailsafe(0));
        Assert.Equal(64, SettingsLimits.ClampFailsafe(400));
        Assert.Equal("09:00", SettingsLimits.ClampSchedule("9:00", "00:00"));
        Assert.Equal("09:00", SettingsLimits.ClampSchedule("nope", "09:00"));
    }

    [Fact]
    public void WiredSettingsRejectOutOfRangeValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-limits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var settings = new SettingsService(Path.Combine(dir, "settings.json"));
            settings.Load();
            settings.Current.Jiggler.Amplitude = 999;
            settings.Current.Jiggler.RadiusPx = 2;
            settings.Current.General.FailsafeSize = 1;
            Assert.Equal(50, settings.Current.Jiggler.Amplitude);
            Assert.Equal(8, settings.Current.Jiggler.RadiusPx);
            Assert.Equal(4, settings.Current.General.FailsafeSize);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CorruptFileResetsAndKeepsBackup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-reset-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        try
        {
            File.WriteAllText(path, "{ not json");
            using var settings = new SettingsService(path);
            settings.Load();
            Assert.True(settings.LastLoadReset);
            Assert.True(File.Exists(path + ".bak"));
            Assert.Equal(8, settings.Current.Jiggler.Amplitude);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class UiUpdateGateTests
{
    [Fact]
    public void ThrottlesUntilTheIntervalElapses()
    {
        var gate = new UiUpdateGate();
        Assert.True(gate.Allow(1_000, 100));
        Assert.False(gate.Allow(1_050, 100));
        Assert.True(gate.Allow(1_100, 100));
    }
}

public class SettingsVersionTests
{
    [Fact]
    public void MissingVersionBecomesCurrentAndWriteLeavesNoTempFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-version-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        try
        {
            File.WriteAllText(path, """{ "jiggler": { "amplitude": 12 } }""");
            using (var settings = new SettingsService(path))
            {
                settings.Load();
                Assert.Equal(AppSettings.CurrentVersion, settings.Current.Version);
                Assert.Equal(12, settings.Current.Jiggler.Amplitude);
                settings.SaveNow();
            }

            Assert.False(File.Exists(path + ".tmp"));
            using var reader = new SettingsService(path);
            reader.Load();
            Assert.Equal(AppSettings.CurrentVersion, reader.Current.Version);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class PointerButtonTests
{
    [Fact]
    public void DescribesSideButtonsAndWheel()
    {
        Assert.Equal((PointerButtons.XDown, 1u << 16), PointerButtons.Describe(ClickButton.X1, down: true));
        Assert.Equal((PointerButtons.XUp, 2u << 16), PointerButtons.Describe(ClickButton.X2, down: false));
        Assert.Equal((PointerButtons.Wheel, (uint)PointerButtons.WheelDelta), PointerButtons.Describe(ClickButton.WheelUp, true));
        Assert.Equal((PointerButtons.Wheel, unchecked((uint)-PointerButtons.WheelDelta)), PointerButtons.Describe(ClickButton.WheelDown, true));
        Assert.True(PointerButtons.IsWheel(ClickButton.WheelUp));
        Assert.False(PointerButtons.IsWheel(ClickButton.Left));
    }
}

public class ClickSequenceTests
{
    [Fact]
    public void WrapsToTheFirstPoint()
    {
        Assert.Equal(1, ClickSequence.NextIndex(0, 3));
        Assert.Equal(0, ClickSequence.NextIndex(2, 3));
        Assert.Equal(0, ClickSequence.NextIndex(0, 0));
    }
}

public class ReleaseVersionTests
{
    [Fact]
    public void DetectsANewerTag()
    {
        var current = new Version(1, 0, 0);
        Assert.True(ReleaseVersion.IsNewer("v1.2.0", current));
        Assert.False(ReleaseVersion.IsNewer("v1.0.0", current));
        Assert.False(ReleaseVersion.IsNewer("nope", current));
    }
}

public class ProfileApplicatorTests
{
    [Fact]
    public void CopiesClickerValuesOntoTheLiveSettings()
    {
        var source = new AppSettings();
        source.Clicker.IntervalMs = 80;
        source.Clicker.Button = ClickButton.X1;
        source.Clicker.Points.Add(new ClickPoint { X = 4, Y = 5 });
        source.Hotkeys.Panic.VirtualKey = 0x1B;
        var profile = ProfileApplicator.Capture(source, "Desk");
        var live = new AppSettings();
        var same = live.Clicker;
        ProfileApplicator.Apply(profile, live);
        Assert.Same(same, live.Clicker);
        Assert.Equal(80, live.Clicker.IntervalMs);
        Assert.Equal(ClickButton.X1, live.Clicker.Button);
        Assert.Equal(4, live.Clicker.Points[0].X);
        Assert.Equal(0x1B, live.Hotkeys.Panic.VirtualKey);
        Assert.Equal("Desk", profile.Name);
    }
}

public class MonitorCornerTests
{
    [Fact]
    public void UsesTheMonitorUnderTheCursorRatherThanTheVirtualDesktopOrigin()
    {
        var leftMonitor = new PixelRect(-1920, 0, 0, 1080);
        var primary = new PixelRect(0, 0, 1920, 1080);

        Assert.True(FailsafeRegion.IsInCorner(-1918, 2, leftMonitor, FailsafeCorner.TopLeft, 8));
        Assert.False(FailsafeRegion.IsInCorner(2, 2, leftMonitor, FailsafeCorner.TopLeft, 8));
        Assert.True(FailsafeRegion.IsInCorner(2, 2, primary, FailsafeCorner.TopLeft, 8));
        Assert.True(FailsafeRegion.IsInCorner(1916, 4, primary, FailsafeCorner.TopRight, 8));
        Assert.True(FailsafeRegion.IsInCorner(3, 1076, primary, FailsafeCorner.BottomLeft, 8));
        Assert.False(FailsafeRegion.IsInCorner(-10, -10, primary, FailsafeCorner.TopLeft, 8));
    }
}
