using Takt.Models;

namespace Takt.Helpers;

public static class ProfileApplicator
{
    public static ActivityProfile Capture(AppSettings settings, string name)
    {
        var profile = new ActivityProfile { Name = string.IsNullOrWhiteSpace(name) ? "Standard" : name.Trim() };
        CopyJiggler(settings.Jiggler, profile.Jiggler);
        CopyClicker(settings.Clicker, profile.Clicker);
        CopyHotkeys(settings.Hotkeys, profile.Hotkeys);
        CopyMacro(settings.Macro, profile.Macro);
        CopyTyper(settings.Typer, profile.Typer);
        CopyPresence(settings.Presence, profile.Presence);
        return profile;
    }

    public static void Apply(ActivityProfile profile, AppSettings settings)
    {
        CopyJiggler(profile.Jiggler, settings.Jiggler);
        CopyClicker(profile.Clicker, settings.Clicker);
        CopyHotkeys(profile.Hotkeys, settings.Hotkeys);
        CopyMacro(profile.Macro, settings.Macro);
        CopyTyper(profile.Typer, settings.Typer);
        CopyPresence(profile.Presence, settings.Presence);
    }

    public static void CopyPresence(PresenceSettings from, PresenceSettings to)
    {
        if (from is null || to is null)
            return;
        to.Enabled = from.Enabled;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.UseFallbackKey = from.UseFallbackKey;
        to.FallbackKey = from.FallbackKey;
        to.UseWorkHours = from.UseWorkHours;
        to.Monday = from.Monday;
        to.Tuesday = from.Tuesday;
        to.Wednesday = from.Wednesday;
        to.Thursday = from.Thursday;
        to.Friday = from.Friday;
        to.Saturday = from.Saturday;
        to.Sunday = from.Sunday;
        to.ScheduleStart = from.ScheduleStart;
        to.ScheduleEnd = from.ScheduleEnd;
    }

    public static void CopyTyper(TyperSettings from, TyperSettings to)
    {
        if (from is null || to is null)
            return;
        to.Text = from.Text;
        to.CharactersPerMinute = from.CharactersPerMinute;
        to.JitterPercent = from.JitterPercent;
        to.StartDelayMs = from.StartDelayMs;
        to.RepeatMode = from.RepeatMode;
        to.RepeatCount = from.RepeatCount;
        to.DurationSeconds = from.DurationSeconds;
        to.Snippets ??= new();
        to.Snippets.Clear();
        if (from.Snippets is null)
            return;
        foreach (var snippet in from.Snippets)
            to.Snippets.Add(new TextSnippet { Name = snippet.Name, Text = snippet.Text });
    }

    public static void CopyJiggler(JigglerSettings from, JigglerSettings to)
    {
        to.Mode = from.Mode;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.Amplitude = from.Amplitude;
        to.RadiusPx = from.RadiusPx;
        to.UseFixedCenter = from.UseFixedCenter;
        to.CenterX = from.CenterX;
        to.CenterY = from.CenterY;
        to.PreventSleep = from.PreventSleep;
        to.PreventDisplayOff = from.PreventDisplayOff;
        to.WaitForIdle = from.WaitForIdle;
        to.IdleThresholdSeconds = from.IdleThresholdSeconds;
        to.PauseOnUserInput = from.PauseOnUserInput;
        to.PauseWhenFullscreen = from.PauseWhenFullscreen;
        to.UseSchedule = from.UseSchedule;
        to.ScheduleStart = from.ScheduleStart;
        to.ScheduleEnd = from.ScheduleEnd;
    }

    public static void CopyClicker(ClickerSettings from, ClickerSettings to)
    {
        to.Button = from.Button;
        to.Action = from.Action;
        to.IntervalMs = from.IntervalMs;
        to.JitterMs = from.JitterMs;
        to.BurstSize = from.BurstSize;
        to.BurstPauseMs = from.BurstPauseMs;
        to.RepeatMode = from.RepeatMode;
        to.RepeatCount = from.RepeatCount;
        to.DurationSeconds = from.DurationSeconds;
        to.StartDelayMs = from.StartDelayMs;
        to.PositionMode = from.PositionMode;
        to.FixedX = from.FixedX;
        to.FixedY = from.FixedY;
        to.Points ??= new();
        to.Points.Clear();
        if (from.Points is null)
            return;
        foreach (var point in from.Points)
            to.Points.Add(new ClickPoint { X = point.X, Y = point.Y });
    }

    public static void CopyHotkeys(HotkeySettings from, HotkeySettings to)
    {
        CopyBinding(from.ToggleJiggler, to.ToggleJiggler);
        CopyBinding(from.ToggleClicker, to.ToggleClicker);
        CopyBinding(from.ToggleBoth, to.ToggleBoth);
        CopyBinding(from.Panic, to.Panic);
        CopyBinding(from.PickPosition, to.PickPosition);
        CopyBinding(from.ToggleMacro, to.ToggleMacro);
        CopyBinding(from.ToggleTyper, to.ToggleTyper);
    }

    public static void CopyMacro(MacroSettings from, MacroSettings to)
    {
        to.Events ??= new();
        to.Events.Clear();
        if (from.Events is null)
            return;
        foreach (var item in from.Events)
        {
            to.Events.Add(new MacroEvent
            {
                DelayMs = item.DelayMs,
                X = item.X,
                Y = item.Y,
                Kind = item.Kind,
                Button = item.Button,
                WheelDelta = item.WheelDelta
            });
        }
    }

    private static void CopyBinding(HotkeyBinding? from, HotkeyBinding to)
    {
        if (from is null)
            return;
        to.Modifiers = from.Modifiers;
        to.VirtualKey = from.VirtualKey;
    }
}
