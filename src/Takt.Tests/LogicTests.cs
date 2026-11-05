using System.IO;
using Takt.Helpers;
using Takt.Models;
using Takt.Services;

namespace Takt.Tests;

public class CoordinateMapperTests
{
    [Fact]
    public void MapsOriginOfVirtualScreenToZero()
    {
        var screen = new VirtualScreen(-1920, 0, 3840, 1080);
        var mapped = CoordinateMapper.ToAbsolute65535(-1920, 0, screen);
        Assert.Equal(0, mapped.X);
        Assert.Equal(0, mapped.Y);
    }

    [Fact]
    public void MapsBottomRightNearMax()
    {
        var screen = new VirtualScreen(0, 0, 1920, 1080);
        var mapped = CoordinateMapper.ToAbsolute65535(1919, 1079, screen);
        Assert.Equal(65535, mapped.X);
        Assert.Equal(65535, mapped.Y);
    }

    [Fact]
    public void ClampsOutOfRange()
    {
        var screen = new VirtualScreen(0, 0, 100, 100);
        var mapped = CoordinateMapper.ToAbsolute65535(10_000, -50, screen);
        Assert.InRange(mapped.X, 0, 65535);
        Assert.InRange(mapped.Y, 0, 65535);
    }
}

public class IntervalCalculatorTests
{
    [Fact]
    public void WithoutJitterReturnsInterval()
    {
        Assert.Equal(100, IntervalCalculator.NextDelayMs(100, 0, new Random(1)));
    }

    [Fact]
    public void NeverGoesBelowOneMillisecond()
    {
        var rng = new Random(42);
        for (var i = 0; i < 200; i++)
            Assert.True(IntervalCalculator.NextDelayMs(5, 400, rng) >= 1);
    }

    [Fact]
    public void JitterStaysAroundInterval()
    {
        var rng = new Random(7);
        var values = Enumerable.Range(0, 50).Select(_ => IntervalCalculator.NextDelayMs(100, 10, rng)).ToArray();
        Assert.All(values, v => Assert.InRange(v, 90, 110));
        Assert.Contains(values, v => v != 100);
    }
}

public class FailsafeRegionTests
{
    private static readonly VirtualScreen Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void DetectsTopLeft()
    {
        Assert.True(FailsafeRegion.IsInCorner(2, 2, Screen, FailsafeCorner.TopLeft, 8));
        Assert.False(FailsafeRegion.IsInCorner(40, 40, Screen, FailsafeCorner.TopLeft, 8));
    }

    [Fact]
    public void DetectsBottomRight()
    {
        Assert.True(FailsafeRegion.IsInCorner(1919, 1079, Screen, FailsafeCorner.BottomRight, 8));
        Assert.False(FailsafeRegion.IsInCorner(100, 100, Screen, FailsafeCorner.BottomRight, 8));
    }
}

public class ScheduleWindowTests
{
    [Fact]
    public void SameDayWindow()
    {
        var noon = new DateTime(2026, 9, 3, 12, 0, 0);
        Assert.True(ScheduleWindow.IsInside(noon, "09:00", "18:00"));
        Assert.False(ScheduleWindow.IsInside(noon, "18:00", "20:00"));
    }

    [Fact]
    public void OvernightWindow()
    {
        var evening = new DateTime(2026, 9, 3, 22, 0, 0);
        var morning = new DateTime(2026, 9, 4, 6, 0, 0);
        Assert.True(ScheduleWindow.IsInside(evening, "21:00", "07:00"));
        Assert.True(ScheduleWindow.IsInside(morning, "21:00", "07:00"));
        Assert.False(ScheduleWindow.IsInside(new DateTime(2026, 9, 3, 12, 0, 0), "21:00", "07:00"));
    }
}

public class SettingsRoundtripTests
{
    [Fact]
    public void SerializesAndRestoresNestedSettings()
    {
        var dir = Path.Combine(Path.GetTempPath(), "takt-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        try
        {
            using (var writer = new SettingsService(path))
            {
                writer.Load();
                writer.Current.Jiggler.Amplitude = 12;
                writer.Current.Clicker.IntervalMs = 42;
                writer.Current.Appearance.AccentHex = "#FF8800";
                writer.Current.Hotkeys.Panic.VirtualKey = 0x1B;
                writer.SaveNow();
            }

            using var reader = new SettingsService(path);
            reader.Load();
            Assert.Equal(12, reader.Current.Jiggler.Amplitude);
            Assert.Equal(42, reader.Current.Clicker.IntervalMs);
            Assert.Equal("#FF8800", reader.Current.Appearance.AccentHex);
            Assert.Equal(0x1B, reader.Current.Hotkeys.Panic.VirtualKey);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

public class UserActivityFilterTests
{
    [Fact]
    public void IgnoresTinyMouseJitter()
    {
        Assert.False(UserActivityFilter.IsSignificantMove(100, 100, 103, 102));
    }

    [Fact]
    public void DetectsRealMouseTravel()
    {
        Assert.True(UserActivityFilter.IsSignificantMove(100, 100, 120, 100));
    }

    [Fact]
    public void TreatsClicksAsActivity()
    {
        Assert.True(UserActivityFilter.IsButtonOrWheel(0x0201));
        Assert.False(UserActivityFilter.IsButtonOrWheel(0x0200));
        Assert.True(UserActivityFilter.IsMouseMove(0x0200));
    }

    [Fact]
    public void DetectsInjectedMouseFlags()
    {
        Assert.True(UserActivityFilter.IsInjected(0x00000001, keyboard: false));
        Assert.True(UserActivityFilter.IsInjected(0x00000002, keyboard: false));
        Assert.False(UserActivityFilter.IsInjected(0, keyboard: false));
    }

    [Fact]
    public void DetectsInjectedKeyboardFlagsWithoutTreatingExtendedKeysAsInjected()
    {
        Assert.True(UserActivityFilter.IsInjected(UserActivityFilter.KeyboardInjected, keyboard: true));
        Assert.True(UserActivityFilter.IsInjected(UserActivityFilter.KeyboardLowerIlInjected, keyboard: true));
        Assert.False(UserActivityFilter.IsInjected(UserActivityFilter.KeyboardExtended, keyboard: true));
        Assert.False(UserActivityFilter.IsInjected(0, keyboard: true));
    }
}

public class NativeLayoutTests
{
    [Fact]
    public void SendInputStructMatchesWin32X64Layout()
    {
        Assert.Equal(40, System.Runtime.InteropServices.Marshal.SizeOf<Takt.Native.Win32.INPUT>());
        Assert.Equal(32, System.Runtime.InteropServices.Marshal.SizeOf<Takt.Native.Win32.MOUSEINPUT>());
    }
}

public class RadiusAreaTests
{
    [Fact]
    public void ContainsPointsInsideCircle()
    {
        Assert.True(RadiusArea.Contains(100, 100, 100, 100, 20));
        Assert.True(RadiusArea.Contains(100, 100, 110, 100, 20));
        Assert.False(RadiusArea.Contains(100, 100, 140, 100, 20));
    }

    [Fact]
    public void ClampsOutsidePointOntoCircle()
    {
        var clamped = RadiusArea.Clamp(0, 0, 100, 0, 20);
        Assert.Equal(20, clamped.X);
        Assert.Equal(0, clamped.Y);
    }

    [Fact]
    public void LeavesInsidePointUnchanged()
    {
        var clamped = RadiusArea.Clamp(50, 50, 55, 50, 20);
        Assert.Equal(55, clamped.X);
        Assert.Equal(50, clamped.Y);
    }

    [Fact]
    public void RandomPointsStayInsideRadius()
    {
        var rng = new Random(3);
        for (var i = 0; i < 80; i++)
        {
            var point = RadiusArea.RandomInCircle(10, 10, 25, rng);
            Assert.True(RadiusArea.Contains(10, 10, point.X, point.Y, 26));
        }
    }
}

