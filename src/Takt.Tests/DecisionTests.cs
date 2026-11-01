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
